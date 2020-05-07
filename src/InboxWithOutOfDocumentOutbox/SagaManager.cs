using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

public class SagaManager : ISagaManager
{
    ISagaPersister sagaPersister;
    IOutboxStore outboxStore;
    IDispatchMessages dispatcher;

    public SagaManager(ISagaPersister sagaPersister, IOutboxStore outboxStore, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
        this.outboxStore = outboxStore;
        this.dispatcher = dispatcher;
    }

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new()
    {
        var sagaState = await LoadSagaState<T>(correlationId);

        if (sagaState.TransactionId != null)
        {
            await FinishTransaction(sagaState.TransactionId.Value, sagaState);
        }

        var outboxState = await outboxStore.Get(messageId);

        if (outboxState == null)
        {
            var transactionId = Guid.NewGuid();

            var sagaDataInstance = (T)sagaState.SagaData ?? new T();
            var (newSagaData, outputMessages) =
                await handlerCallback(sagaDataInstance, context).ConfigureAwait(false);

            sagaState.SagaData = newSagaData;
            sagaState.TransactionId = transactionId;

            outboxState = new OutboxState
            {
                OutgoingMessages = outputMessages.Operations.Serialize()
            };
            await outboxStore.Store(transactionId, messageId, outboxState);

            await sagaPersister.Persist(sagaState);

            await FinishTransaction(transactionId, sagaState);
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

    async Task FinishTransaction(Guid transactionId, SagaDataContainer sagaState)
    {
        await outboxStore.Commit(transactionId);

        sagaState.TransactionId = null;
        await sagaPersister.Persist(sagaState);
    }

    async Task<SagaDataContainer> LoadSagaState<T>(string correlationId) where T : class, new()
    {
        return await sagaPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false)
               ?? new SagaDataContainer { Id = correlationId };
    }
}