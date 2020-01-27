using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

public class SagaManager
{
    ISagaPersister sagaPersister;
    ITokenStore tokenStore;
    IDispatchMessages dispatcher;

    public SagaManager(ISagaPersister sagaPersister, ITokenStore tokenStore, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
        this.tokenStore = tokenStore;
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

        var token = await tokenStore.Get(messageId).ConfigureAwait(false);
        if (token == null || token.ClaimedBy != outboxState.ClaimId)
        {
            return; //Duplicate
        }

        if (token.ClaimedBy == null)
        {
            token.ClaimedBy = outboxState.ClaimId;
            await tokenStore.Update(token).ConfigureAwait(false);
        }

        if (outboxState.OutgoingMessages == null)
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

        await tokenStore.Delete(token).ConfigureAwait(false);

        sagaContainer.OutboxState.Remove(messageId);
        await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
    }
}