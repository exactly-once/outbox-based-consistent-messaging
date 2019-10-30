using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

public class SagaManager
{
    ISagaPersister sagaPersister;
    IInboxStore inboxStore;
    IDispatchMessages dispatcher;

    public SagaManager(ISagaPersister sagaPersister, IInboxStore inboxStore, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
        this.inboxStore = inboxStore;
        this.dispatcher = dispatcher;
    }

    public Task Process<T>(string correlationId, IMessageHandlerContext context, Func<T, Task<T>> handlerCallback)
        where T : class, new()
    {
        return Process(context.MessageId, correlationId, context.Extensions, (Func<T, ContextBag, Task<(T, PendingTransportOperations)>>) (async (sagaData, bag) =>
        {
            var pendingTransportOperations = new PendingTransportOperations();
            bag.Set(pendingTransportOperations); //override the one set by the outbox

            var newSagaData = await handlerCallback(sagaData).ConfigureAwait(false);

            bag.Remove<PendingTransportOperations>(); //Restore old value
            return (newSagaData, pendingTransportOperations);
        }));
    }

    public async Task Process<T>(string messageId, string correlationId, ContextBag context, 
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback) 
        where T : class, new()
    {
        var sagaContainer = await sagaPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false) 
                            ?? new SagaDataContainer(){Id = correlationId};

        if (!sagaContainer.OutboxState.TryGetValue(messageId, out var outboxState))
        {
            outboxState = new OutboxState
            {
                ClaimId = Guid.NewGuid()
            };
            sagaContainer.OutboxState[messageId] = outboxState;

            await sagaPersister.StoreClaimId(sagaContainer).ConfigureAwait(false);
        }
        var deduplicationResult = await inboxStore.Deduplicate(messageId, outboxState.ClaimId).ConfigureAwait(false);

        //Execute business logic if we managed to claim the inbox of if we claimed it before but haven't executed the business logic
        if (deduplicationResult == DeduplicateResult.RecordCreated 
            || deduplicationResult == DeduplicateResult.RecordExists && outboxState.OutgoingMessages == null)
        {
            var sagaDataInstance = (T)sagaContainer.SagaData ?? new T();

            var (newSagaData, pendingTransportOperations) = await handlerCallback(sagaDataInstance, context).ConfigureAwait(false);

            sagaContainer.SagaData = newSagaData;
            outboxState.OutgoingMessages = pendingTransportOperations.Operations.Serialize();

            await sagaPersister.PersistState(sagaContainer).ConfigureAwait(false);
        }

        if (outboxState.OutgoingMessages != null)
        {
            var toDispatch = outboxState.OutgoingMessages.Deserialize();
            await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context).ConfigureAwait(false);
        }

        sagaContainer.OutboxState.Remove(messageId);
        if (sagaContainer.SagaData == null)
        {
            //Empty saga container. Saga has already completed.
            await sagaPersister.Delete(sagaContainer).ConfigureAwait(false);
        }
        else
        {
            await sagaPersister.MarkDispatched(sagaContainer).ConfigureAwait(false);
        }
    }
}