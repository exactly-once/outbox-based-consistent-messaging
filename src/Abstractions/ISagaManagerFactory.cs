using System;
using System.Threading.Tasks;
using NServiceBus.Transport;

public interface ISagaManagerFactory
{
    ISagaManager Create(Func<string, Task> barrierCallback, IDispatchMessages dispatcher);
    Task<object> LoadSaga(string sagaId);
}