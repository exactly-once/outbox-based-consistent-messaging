using System;
using System.Threading.Tasks;
using NServiceBus.Transport;

public class TokenBasedWithExternalOutboxSagaManagerFactory : ISagaManagerFactory
{
    ISagaPersister persister = new InMemorySagaPersister();
    ITokenStore tokenStore = new InMemoryTokenStore();
    IOutboxStore outbox = new InMemoryOutbox();
    ITransientMessageStateStore transientMessageStateStore = new InMemoryTransientMessageStateStore();

    public ISagaManager Create(Func<string, Task> barrierCallback, IDispatchMessages dispatcher)
    {
        return new SagaManager(
            new TestingSagaDataPersister(barrierCallback, persister),
            new TestingTokenStore(barrierCallback, tokenStore),
            new TestingOutbox(barrierCallback, outbox),
            transientMessageStateStore, dispatcher);
    }

    public async Task<object> LoadSaga(string sagaId)
    {
        var container = await persister.LoadByCorrelationId(sagaId).ConfigureAwait(false);
        return container?.SagaData;
    }

    public void PrepareMessage(string messageId)
    {
        tokenStore.Create(messageId);
    }
}