using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

public class SagaManager : ISagaManager
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

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new()
    {
        var sagaContainer = await sagaPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false)
                            ?? new SagaDataContainer { Id = correlationId };

        TransportOperation[] outgoingMessages;
        if (!sagaContainer.OutboxState.ContainsKey(messageId))
        {
            var hasBeenProcessed = await inboxStore.HasBeenProcessed(messageId).ConfigureAwait(false);
            if (hasBeenProcessed)
            {
                return;
            }

            var sagaDataInstance = (T)sagaContainer.SagaData ?? new T();
            var (newSagaData, pendingTransportOperations) =
                await handlerCallback(sagaDataInstance, context).ConfigureAwait(false);

            outgoingMessages = pendingTransportOperations.Operations.Serialize();

            sagaContainer.SagaData = newSagaData;
            sagaContainer.OutboxState[messageId] = new OutboxState
            {
                OutgoingMessages = outgoingMessages
            };

            await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        }
        else
        {
            outgoingMessages = sagaContainer.OutboxState[messageId].OutgoingMessages;
        }

        var toDispatch = outgoingMessages.Deserialize();
        await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context).ConfigureAwait(false);

        await inboxStore.MarkProcessed(messageId).ConfigureAwait(false);

        sagaContainer.OutboxState.Remove(messageId);

        await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
    }
}