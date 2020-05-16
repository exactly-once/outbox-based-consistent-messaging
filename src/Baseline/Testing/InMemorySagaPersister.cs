using System.Threading.Tasks;

class InMemorySagaPersister : ConsistentInMemoryStore<Entity>, ISagaPersister
{
    public Task<Entity> LoadByCorrelationId(string correlationId)
    {
        return Get(correlationId);
    }

    public Task Persist(Entity sagaContainer)
    {
        return Put(sagaContainer);
    }
}