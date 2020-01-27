using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using TransportOperation = NServiceBus.Outbox.TransportOperation;

public class SagaManager : ISagaManager
{
    ISagaPersister sagaPersister;
    IDispatchMessages dispatcher;

    public SagaManager(ISagaPersister sagaPersister, IDispatchMessages dispatcher)
    {
        this.sagaPersister = sagaPersister;
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
            //Message has not been processed yet
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

        if (outgoingMessages != null)
        {
            var toDispatch = outgoingMessages.Deserialize();
            await dispatcher.Dispatch(new TransportOperations(toDispatch), new TransportTransaction(), context).ConfigureAwait(false);

            sagaContainer.OutboxState[messageId].OutgoingMessages = null;

            await sagaPersister.Persist(sagaContainer).ConfigureAwait(false);
        }
    }
}