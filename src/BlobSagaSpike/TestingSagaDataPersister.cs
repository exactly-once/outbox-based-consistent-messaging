using System;
using System.Threading.Tasks;

class TestingSagaDataPersister : ISagaPersister, IInboxStore
{
    IInboxStore realInbox;
    ISagaPersister realPersister;
    char threadId;
    Func<char, bool, Task> getBarrier;
    bool starting = true;

    public TestingSagaDataPersister(ISagaPersister realPersister, IInboxStore realInbox, char threadId, Func<char, bool, Task> getBarrier)
    {
        this.realPersister = realPersister;
        this.realInbox = realInbox;
        this.threadId = threadId;
        this.getBarrier = getBarrier;
    }

    public async Task<DeduplicateResult> Deduplicate(string messageId, Guid claimId)
    {
        await Barrier().ConfigureAwait(false);
        return await realInbox.Deduplicate(messageId, claimId).ConfigureAwait(false);
    }

    public async Task<SagaDataContainer> LoadByCorrelationId(string correlationId)
    {
        await Barrier().ConfigureAwait(false);
        return await realPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false);
    }

    public async Task StoreClaimId(SagaDataContainer sagaContainer)
    {
        await Barrier().ConfigureAwait(false);
        await realPersister.StoreClaimId(sagaContainer).ConfigureAwait(false);
    }

    public async Task PersistState(SagaDataContainer sagaContainer)
    {
        await Barrier().ConfigureAwait(false);
        await realPersister.PersistState(sagaContainer).ConfigureAwait(false);
    }

    public async Task MarkDispatched(SagaDataContainer sagaContainer)
    {
        await Barrier().ConfigureAwait(false);
        await realPersister.MarkDispatched(sagaContainer).ConfigureAwait(false);
    }

    public async Task Delete(SagaDataContainer sagaContainer)
    {
        await Barrier().ConfigureAwait(false);
        await realPersister.Delete(sagaContainer).ConfigureAwait(false);
    }
    Task Barrier()
    {
        var barrier = getBarrier(threadId, starting);
        starting = false;
        return barrier;
    }
}