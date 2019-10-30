using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Transport;

class FakeDispatcher : IDispatchMessages
{
    public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
    {
        return Task.CompletedTask;
    }
}