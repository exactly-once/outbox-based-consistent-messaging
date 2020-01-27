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
    IOutboxStore outboxStore;
    IDispatchMessages dispatcher;

    public SagaManager(ISagaPersister sagaPersister, IInboxStore inboxStore, IOutboxStore outboxStore, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
        this.inboxStore = inboxStore;
        this.outboxStore = outboxStore;
        this.dispatcher = dispatcher;
    }

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new()
    {
        var sagaContainer = await sagaPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false)
                            ?? new SagaDataContainer { Id = correlationId };

        TransportOperation[] outgoingMessages;
        string outboxKey;
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

            outboxKey = Guid.NewGuid().ToString();
            outgoingMessages = pendingTransportOperations.Operations.Serialize();

            await outboxStore.Store(outboxKey, new OutboxState
            {
                OutgoingMessages = outgoingMessages
            });

            sagaContainer.SagaData = newSagaData;
            sagaContainer.OutboxState[messageId] = outboxKey;

            await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        }
        else
        {
            outboxKey = sagaContainer.OutboxState[messageId];
            var outboxState = await outboxStore.Get(outboxKey).ConfigureAwait(false);
            outgoingMessages = outboxState?.OutgoingMessages;
        }

        if (outgoingMessages != null)
        {
            await dispatcher
                .Dispatch(new TransportOperations(outgoingMessages.Deserialize()), new TransportTransaction(), context)
                .ConfigureAwait(false);

            await outboxStore.Remove(outboxKey).ConfigureAwait(false);
        }

        await inboxStore.MarkProcessed(messageId).ConfigureAwait(false);

        sagaContainer.OutboxState.Remove(messageId);

        await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
    }
}