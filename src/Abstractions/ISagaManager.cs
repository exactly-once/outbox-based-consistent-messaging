using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;

public interface ISagaManager
{
    Task Process<T>(string messageId, string correlationId, ContextBag context,
        Func<T, ContextBag, Task<(T, PendingTransportOperations)>> handlerCallback)
        where T : class, new();
}