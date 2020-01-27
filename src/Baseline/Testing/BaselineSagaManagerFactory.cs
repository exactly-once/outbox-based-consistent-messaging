using System;
using System.Threading.Tasks;
using NServiceBus.Transport;

public class BaselineSagaManagerFactory : ISagaManagerFactory
{
    ISagaPersister persister = new InMemorySagaPersister();

    public ISagaManager Create(Func<string, Task> barrierCallback, IDispatchMessages dispatcher)
    {
        return new SagaManager(new TestingSagaDataPersister(barrierCallback, persister), dispatcher);
    }

    public async Task<object> LoadSaga(string sagaId)
    {
        var container = await persister.LoadByCorrelationId(sagaId).ConfigureAwait(false);
        return container?.SagaData;
    }
}