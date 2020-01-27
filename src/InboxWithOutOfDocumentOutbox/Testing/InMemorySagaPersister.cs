using System;
using System.Threading.Tasks;

class InMemorySagaPersister : ConsistentInMemoryStore<SagaDataContainer>, ISagaPersister
{
    public Task<SagaDataContainer> LoadByCorrelationId(string correlationId)
    {
        return Get(correlationId);
    }

    public Task Persist(SagaDataContainer sagaContainer)
    {
        return Put(sagaContainer);
    }
}