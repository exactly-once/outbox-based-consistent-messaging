using System;
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

    public async Task<SagaDataContainer> LoadByCorrelationId(string correlationId)
    {
        var result = await impl.LoadByCorrelationId(correlationId).ConfigureAwait(false);
        await barrier("Saga.Load").ConfigureAwait(false);
        return result;
    }

    public async Task Persist(SagaDataContainer sagaContainer)
    {
        await impl.Persist(sagaContainer).ConfigureAwait(false);
        await barrier("Saga.Persist").ConfigureAwait(false);
    }
}