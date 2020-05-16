using System;
using System.Threading.Tasks;
using NServiceBus.Transport;

public class BasicInboxSagaManagerFactory : ISagaManagerFactory
{
    ISagaPersister persister = new InMemorySagaPersister();
    IInboxStore inbox = new InMemoryInbox();

    public ISagaManager Create(Func<string, Task> barrierCallback, IDispatchMessages dispatcher)
    {
        return new SagaManager(new TestingSagaDataPersister(barrierCallback, persister),
            new TestingInbox(barrierCallback, inbox), dispatcher);
    }

    public async Task<object> LoadSaga(string sagaId)
    {
        var container = await persister.LoadByCorrelationId(sagaId).ConfigureAwait(false);
        return container?.SagaData;
    }

    public void PrepareMessage(string messageId)
    {
    }
}