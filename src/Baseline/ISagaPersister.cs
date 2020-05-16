using System.Threading.Tasks;

public interface ISagaPersister
{
    Task<Entity> LoadByCorrelationId(string correlationId);
    Task Persist(Entity sagaContainer);
}