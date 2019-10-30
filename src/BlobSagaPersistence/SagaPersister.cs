using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NServiceBus.Transport;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

public class StorageSessionAdapter : ISynchronizedStorageAdapter
{
    public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
    {
        var outboxTransaction = (BlobSagaOutboxTransaction)transaction;
        return Task.FromResult<CompletableSynchronizedStorageSession>(
            new BlobSagaSynchronizedStorageSession(outboxTransaction));
    }

    public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
    {
        throw new NotImplementedException();
    }
}

public class BlobSagaSynchronizedStorageSession : CompletableSynchronizedStorageSession
{
    BlobSagaOutboxTransaction outboxTransaction;

    public BlobSagaSynchronizedStorageSession(BlobSagaOutboxTransaction outboxTransaction)
    {
        this.outboxTransaction = outboxTransaction;
    }

    public string MessageId => outboxTransaction.MessageId;
    public SagaDataContainer DataContainer { get; private set; }

    public void Initialize(Action<OutboxMessage> onMessagesCaptured, Func<Task> onOutboxCommit, Func<Task> onDispatched)
    {
        outboxTransaction.Initialize(onMessagesCaptured, onOutboxCommit, onDispatched);
    }

    public void Dispose()
    {
        //NOOP
    }

    public Task CompleteAsync()
    {
        //Completion is managed by the outbox
        return Task.CompletedTask;
    }

    public void SkipProcessing()
    {
        throw new NotImplementedException();
    }

    public void AttachExistingDataContainer(SagaDataContainer dataContainer)
    {
        DataContainer = dataContainer;
    }
}

interface IInboxStore
{
    Task<DeduplicateResult> Deduplicate(string messageId, Guid claimId);
}

enum DeduplicateResult
{
    RecordInserted,
    RecordPresent,
    Duplicate
}

class SagaPersister : ISagaPersister
{
    IInboxStore inboxStore;

    public SagaPersister(IInboxStore inboxStore)
    {
        this.inboxStore = inboxStore;
    }

    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
    {
        var dataContainer = await LoadSagaDataContainer(sagaId).ConfigureAwait(false);

        if (dataContainer == null)
        {
            return null;
        }

        var typedSession = (BlobSagaSynchronizedStorageSession)session;
        var messageId = typedSession.MessageId;
        typedSession.AttachExistingDataContainer(dataContainer);

        if (dataContainer.OutboxState.TryGetValue(messageId, out var outboxState))
        {
            if (outboxState.OutgoingMessages != null)
            {
                //We've already processed this message
                typedSession.SkipProcessing();
                //Here we could technically skip inbox-based verification
            }
        }
        else
        {
            //Outbox does not contain this message
            outboxState = new OutboxState
            {
                ClaimId = Guid.NewGuid()
            };
            dataContainer.OutboxState[messageId] = outboxState;

            //Ensure ClaimID is persisted
            await PersistSagaDataContainer(dataContainer);
        }

        //Duplicate if exists an inbox record with a different claim ID
        var deduplicationResult = await inboxStore.Deduplicate(messageId, outboxState.ClaimId).ConfigureAwait(false);
        if (deduplicationResult == DeduplicateResult.Duplicate)
        {
            //It's a duplicate. We don't want to process this message
            typedSession.SkipProcessing();
        }

        return (TSagaData)dataContainer.SagaData;
    }

    public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session,
        ContextBag context)
    {
        //Can be called either if no container exists or if a container does exist but there is no saga data attached to it
        //In the latter case the saga business logic has already been invoked but the data has not been persisted

        var typedSession = (BlobSagaSynchronizedStorageSession)session;
        var messageId = typedSession.MessageId;
        var outboxState = new OutboxState
        {
            ClaimId = Guid.NewGuid()
        };

        var dataContainer = new SagaDataContainer
        {
            SagaData = sagaData,
            OutboxState = { [messageId] = outboxState }
        };

        //We need to persist the container with ClaimID to be able to de-duplicate using inbox
        await PersistSagaDataContainer(dataContainer).ConfigureAwait(false);
        var deduplicationResult = await inboxStore.Deduplicate(messageId, outboxState.ClaimId).ConfigureAwait(false);
        if (deduplicationResult == DeduplicateResult.Duplicate)
        {
            //The message has been already processed but the saga instance has been completed in the meantime
            //- ignore generated outgoing messages
            //- do not persist the state on commit
            //- remove the container on dispatch
            typedSession.Initialize(
                x => {},
                async () =>
                {
                    await PersistSagaDataContainer(dataContainer).ConfigureAwait(false);
                },
                async () =>
                {
                    await DeleteContainer(dataContainer).ConfigureAwait(false);
                });
        }
        else
        {
            //Attach the newly created saga data
            dataContainer.SagaData = sagaData;
            typedSession.Initialize(
                x => outboxState.OutgoingMessages = x.TransportOperations,
                async () =>
                {
                    await PersistSagaDataContainer(dataContainer).ConfigureAwait(false);
                },
                async () =>
                {
                    //De-duplication data is safe in the inbox. We can remove the outbox entry now
                    dataContainer.OutboxState.Remove(messageId);
                    await PersistSagaDataContainer(dataContainer).ConfigureAwait(false);
                });
        }
    }

    

    public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var typedSession = (BlobSagaSynchronizedStorageSession)session;
        typedSession.DataContainer.SagaData = sagaData;

        return Task.CompletedTask;
    }

    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : class, IContainSagaData
    {
        throw new NotImplementedException();
    }

    public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        throw new NotImplementedException();
    }

    Task<SagaDataContainer> LoadSagaDataContainer(Guid sagaId)
    {
        throw new NotImplementedException();
    }

    Task PersistSagaDataContainer(SagaDataContainer dataContainer)
    {
        throw new NotImplementedException();
    }

    Task DeleteContainer(SagaDataContainer dataContainer)
    {
        throw new NotImplementedException();
    }
}

public class SagaDataContainer
{
    public Dictionary<string, OutboxState> OutboxState = new Dictionary<string, OutboxState>();
    public object SagaData { get; set; }
}

public class OutboxState
{
    public Guid ClaimId { get; set; }
    public TransportOperation[] OutgoingMessages { get; set; }
}

public class OutgoingMessage
{
    public string MessageId { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public byte[] Body { get; set; }
}