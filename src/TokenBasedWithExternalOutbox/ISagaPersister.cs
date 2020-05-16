using System.Threading.Tasks;

public interface ISagaPersister
{
    Task<SagaDataContainer> LoadByCorrelationId(string correlationId);
    Task Persist(SagaDataContainer sagaContainer);
}