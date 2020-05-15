using System;
using System.Threading.Tasks;
using InboxWithOutOfDocumentOutbox.Testing;
using NServiceBus.Transport;

public class CosmosDbSagaManagerFactory 
{
    CosmosDbSagaPersister persister;

    public CosmosDbSagaManager Create(IDispatchMessages dispatcher)
    {
        persister = new CosmosDbSagaPersister();
        persister.Initialize().GetAwaiter().GetResult();

        var outbox = new CosmosDbOutbox();

        return new CosmosDbSagaManager(persister, outbox, dispatcher);
    }

    public async Task<T> LoadSaga<T>(string sagaId) where T : E1Content
    {
        var container = await persister.Load<T>(sagaId).ConfigureAwait(false);
        return container?.Item;
    }
}