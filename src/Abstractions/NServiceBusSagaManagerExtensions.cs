using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;

public static class NServiceBusSagaManagerExtensions
{
    public static Task Process<T>(this ISagaManager sagaManager, string correlationId, 
        IMessageHandlerContext context, Func<T, Task<T>> handlerCallback)
        where T : class, new()
    {
        return sagaManager.Process(context.MessageId, correlationId, context.Extensions, (Func<T, ContextBag, Task<(T, PendingTransportOperations)>>)(async (sagaData, bag) =>
        {
            var pendingTransportOperations = new PendingTransportOperations();
            bag.Set(pendingTransportOperations); //override the one set by the outbox

            var newSagaData = await handlerCallback(sagaData).ConfigureAwait(false);

            bag.Remove<PendingTransportOperations>(); //Restore old value
            return (newSagaData, pendingTransportOperations);
        }));
    }
}