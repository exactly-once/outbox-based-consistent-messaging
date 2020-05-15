using System;
using System.Threading.Tasks;
using InboxWithOutOfDocumentOutbox.Testing;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

public class CosmosDbSagaManager
{
    CosmosDbSagaPersister sagaPersister;
    CosmosDbOutbox outboxStore;
    IDispatchMessages dispatcher;

    public CosmosDbSagaManager(CosmosDbSagaPersister sagaPersister, CosmosDbOutbox outboxStore, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
        this.outboxStore = outboxStore;
        this.dispatcher = dispatcher;
    }

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : E1Content, new()
    {
        var state = await LoadSagaState<T>(correlationId);

        if (state.Item.TransactionId != null)
        {
            await FinishTransaction(state.Item.TransactionId.Value, state);
        }

        var outboxState = await outboxStore.Get(messageId);

        if (outboxState == null)
        {
            var sagaDataInstance = state.Item ?? new T();
            var (newSagaData, outputMessages) =
                await handlerCallback(sagaDataInstance, context).ConfigureAwait(false);

            var transactionId = Guid.NewGuid();

            state.Item = newSagaData;
            state.Item.TransactionId = transactionId;

            outboxState = new OutboxState
            {
                Id = transactionId.ToString(),
                MessageId = messageId,
                OutgoingMessages = outputMessages.Operations.Serialize()
            };

            await outboxStore.Store(outboxState);

            await sagaPersister.Persist(state);

            await FinishTransaction(transactionId, state);
        }

        if (outboxState.OutgoingMessages != null)
        {
            var messages = outboxState.OutgoingMessages.Deserialize();

            await dispatcher
                .Dispatch(new TransportOperations(messages), new TransportTransaction(), context)
                .ConfigureAwait(false);

            await outboxStore.CleanMessages(messageId).ConfigureAwait(false);
        }
    }

    async Task FinishTransaction<T>(Guid transactionId, E1Document<T> state) where T : E1Content
    {
        await outboxStore.Commit(transactionId);

        state.Item.TransactionId = null;
        await sagaPersister.Persist<T>(state);
    }

    async Task<E1Document<T>> LoadSagaState<T>(string correlationId) where T : E1Content, new()
    {
        return await sagaPersister.Load<T>(correlationId).ConfigureAwait(false)
               ?? new E1Document<T>{Item = new T { Id = correlationId}};
    }
}