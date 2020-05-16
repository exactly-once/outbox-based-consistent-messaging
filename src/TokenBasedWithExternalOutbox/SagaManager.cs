using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

public class SagaManager : ISagaManager
{
    ISagaPersister sagaPersister;
    ITokenStore tokenStore;
    IDispatchMessages dispatcher;
    IOutboxStore outboxStore;
    ITransientMessageStateStore transientMessageStateStore;

    public SagaManager(ISagaPersister sagaPersister, ITokenStore tokenStore, IOutboxStore outboxStore,
        ITransientMessageStateStore transientMessageStateStore, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
        this.tokenStore = tokenStore;
        this.dispatcher = dispatcher;
        this.outboxStore = outboxStore;
        this.transientMessageStateStore = transientMessageStateStore;
    }

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new()
    {
        var sagaContainer = await sagaPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false)
                            ?? new SagaDataContainer { Id = correlationId };
        
        var tokenExists = await tokenStore.Exists(messageId).ConfigureAwait(false);
        if (!tokenExists)
        {
            return; //Duplicate
        }

        TransportOperation[] toDispatch;
        if (sagaContainer.TransactionId != null)
        {
            if (sagaContainer.TransactionId == messageId)
            {
                return; //Duplicate
            }

            //Dispatch previous message
            var outboxState = await outboxStore.Get(sagaContainer.TransactionId).ConfigureAwait(false);
            toDispatch = outboxState?.OutgoingMessages?.Deserialize() ?? new TransportOperation[0];
            await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context).ConfigureAwait(false);

            //Need to ensure token is removed before TransactionId is modified and persisted
            await outboxStore.Remove(sagaContainer.TransactionId).ConfigureAwait(false);
            await tokenStore.Delete(messageId).ConfigureAwait(false);
            sagaContainer.TransactionId = null;
        }

        if (sagaContainer.TransactionId == null)
        {
            var sagaDataInstance = (T)sagaContainer.SagaData ?? new T();

            var (newSagaData, pendingTransportOperations) = await handlerCallback(sagaDataInstance, context).ConfigureAwait(false);

            sagaContainer.SagaData = newSagaData;

            toDispatch = pendingTransportOperations.Operations;
            var outgoingMessages = toDispatch.Serialize();
            await outboxStore.Store(messageId, new OutboxState
            {
                OutgoingMessages = outgoingMessages
            });

            sagaContainer.TransactionId = messageId;
            await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        }
        else
        {
            var outboxState = await outboxStore.Get(messageId).ConfigureAwait(false);
            toDispatch = outboxState?.OutgoingMessages?.Deserialize() ?? new TransportOperation[0];
        }

        await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context).ConfigureAwait(false);

        await outboxStore.Remove(sagaContainer.TransactionId).ConfigureAwait(false);
        await tokenStore.Delete(messageId).ConfigureAwait(false);

        //
        sagaContainer.TransactionId = null;
        await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        //transientMessageStateStore.MarkProcessed(messageId);
    }
}