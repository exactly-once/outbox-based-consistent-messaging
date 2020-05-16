using System;
using System.Threading.Tasks;
using InboxWithOutOfDocumentOutbox.Testing;
using NServiceBus.Transport;

public class CosmosDBSagaManagerFactory 
{
    CosmosDbSagaPersister persister;

    public async Task<CosmosDbSagaManager> Create(IDispatchMessages dispatcher)
    {
        persister = new CosmosDbSagaPersister();
        await persister.Initialize();

        var outbox = new CosmosDbOutbox();
        await outbox.Initialize();

        return new CosmosDbSagaManager(persister, outbox, dispatcher);
    }

    public async Task<T> LoadSaga<T>(string sagaId) where T : E1Content
    {
        var container = await persister.Load<T>(sagaId).ConfigureAwait(false);
        return container?.Item;
    }
}