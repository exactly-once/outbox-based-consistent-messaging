﻿using System;
using System.Threading.Tasks;

class TestingSagaDataPersister : ISagaPersister
{
    Func<string, Task> barrier;
    ISagaPersister impl;

    public TestingSagaDataPersister(Func<string, Task> barrier, ISagaPersister impl)
    {
        this.barrier = barrier;
        this.impl = impl;
    }

    public async Task<Entity> LoadByCorrelationId(string correlationId)
    {
        await barrier("Saga.Load").ConfigureAwait(false);
        return await impl.LoadByCorrelationId(correlationId).ConfigureAwait(false);
    }

    public async Task Persist(Entity sagaContainer)
    {
        await barrier("Saga.Persist").ConfigureAwait(false);
        await impl.Persist(sagaContainer).ConfigureAwait(false);
    }
}