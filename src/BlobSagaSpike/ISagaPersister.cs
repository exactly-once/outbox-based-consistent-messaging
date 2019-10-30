using System.Threading.Tasks;

public interface ISagaPersister
{
    Task<SagaDataContainer> LoadByCorrelationId(string correlationId);
    Task StoreClaimId(SagaDataContainer sagaContainer);
    Task PersistState(SagaDataContainer sagaContainer);
    Task MarkDispatched(SagaDataContainer sagaContainer);
    Task Delete(SagaDataContainer sagaContainer);
}