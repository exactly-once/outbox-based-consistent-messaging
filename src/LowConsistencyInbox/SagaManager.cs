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

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new()
    {
        var sagaContainer = await sagaPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false)
                            ?? new SagaDataContainer() { Id = correlationId };

        if (!sagaContainer.OutboxState.TryGetValue(messageId, out var outboxState))
        {
            outboxState = new OutboxState
            {
                ClaimId = Guid.NewGuid()
            };
            sagaContainer.OutboxState[messageId] = outboxState;

            await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        }

        var alreadyClaimed = false;
        var newClaimSucceeded = await inboxStore.TryClaim(messageId, outboxState.ClaimId).ConfigureAwait(false);
        if (!newClaimSucceeded)
        {
            alreadyClaimed = await inboxStore.IsClaimedBy(messageId, outboxState.ClaimId).ConfigureAwait(false);
        }

        if (newClaimSucceeded || alreadyClaimed && outboxState.OutgoingMessages == null)
        {
            var sagaDataInstance = (T)sagaContainer.SagaData ?? new T();

            var (newSagaData, pendingTransportOperations) = await handlerCallback(sagaDataInstance, context).ConfigureAwait(false);

            sagaContainer.SagaData = newSagaData;
            outboxState.OutgoingMessages = pendingTransportOperations.Operations.Serialize();

            await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        }

        if (outboxState.OutgoingMessages != null)
        {
            var toDispatch = outboxState.OutgoingMessages.Deserialize();
            await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context).ConfigureAwait(false);
        }

        sagaContainer.OutboxState.Remove(messageId);
        await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
    }
}