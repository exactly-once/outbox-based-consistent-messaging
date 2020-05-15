using System.Threading.Tasks;

public interface ISagaPersister
{
    Task<E1Document<T>> Load<T>(string correlationId) where T : E1Content;
    Task Persist<T>(E1Document<T> document) where T : E1Content;
}