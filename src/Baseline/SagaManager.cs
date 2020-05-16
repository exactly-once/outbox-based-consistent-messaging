using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

public class SagaManager : ISagaManager
{
    ISagaPersister persister;
    IDispatchMessages dispatcher;

    public SagaManager(ISagaPersister persister, IDispatchMessages dispatcher)
    {
        this.persister = persister;
        this.dispatcher = dispatcher;
    }

    public async Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new()
    {
var entity = await persister.LoadByCorrelationId(correlationId).ConfigureAwait(false)
                    ?? new Entity { Id = correlationId };

TransportOperation[] outgoingMessages;
if (!entity.OutboxState.ContainsKey(messageId))
{
    var state = (T)entity.BusinessState ?? new T();

    var (newState, pendingTransportOperations) =
        await handlerCallback(state, context).ConfigureAwait(false);

    outgoingMessages = pendingTransportOperations.Operations.Serialize();

    entity.BusinessState = newState;
    entity.OutboxState[messageId] = new OutboxState
    {
        OutgoingMessages = outgoingMessages
    };

    await persister.Persist(entity).ConfigureAwait(false);
}
else
{
    outgoingMessages = entity.OutboxState[messageId].OutgoingMessages;
}

if (outgoingMessages != null)
{
    var toDispatch = outgoingMessages.Deserialize();
    await Dispatch<T>(context, toDispatch).ConfigureAwait(false);

    entity.OutboxState[messageId].OutgoingMessages = null;

    await persister.Persist(entity).ConfigureAwait(false);
}
    }

    async Task Dispatch<T>(ContextBag context, NServiceBus.Transport.TransportOperation[] toDispatch) where T : class, new()
    {
        await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context)
            .ConfigureAwait(false);
    }
}