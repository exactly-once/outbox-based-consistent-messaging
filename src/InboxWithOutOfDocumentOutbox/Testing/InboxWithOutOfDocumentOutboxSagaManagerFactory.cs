﻿using System;
using System.Threading.Tasks;
using NServiceBus.Transport;

public class InboxWithOutOfDocumentOutboxSagaManagerFactory : ISagaManagerFactory
{
    ISagaPersister persister = new InMemorySagaPersister();
    IInboxStore inbox = new InMemoryInbox();
    IOutboxStore outbox = new InMemoryOutbox();

    public ISagaManager Create(Func<string, Task> barrierCallback, IDispatchMessages dispatcher)
    {
        return new SagaManager(
            new TestingSagaDataPersister(barrierCallback, persister),
            new TestingInbox(barrierCallback, inbox),
            new TestingOutbox(barrierCallback, outbox), 
            dispatcher);
    }

    public async Task<object> LoadSaga(string sagaId)
    {
        var container = await persister.LoadByCorrelationId(sagaId).ConfigureAwait(false);
        return container?.SagaData;
    }
}