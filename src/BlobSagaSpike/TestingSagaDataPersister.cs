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

    public async Task<SagaDataContainer> LoadByCorrelationId(string correlationId)
    {
        await Barrier().ConfigureAwait(false);
        return await realPersister.LoadByCorrelationId(correlationId).ConfigureAwait(false);
    }

    public async Task Persist(SagaDataContainer sagaContainer)
    {
        await Barrier().ConfigureAwait(false);
        await realPersister.Persist(sagaContainer).ConfigureAwait(false);
    }

    public async Task<bool> TryClaim(string messageId, Guid claimId)
    {
        await Barrier().ConfigureAwait(false);
        return await realInbox.TryClaim(messageId, claimId).ConfigureAwait(false);
    }

    public async Task<bool> IsClaimedBy(string messageId, Guid claimId)
    {
        await Barrier().ConfigureAwait(false);
        return await realInbox.IsClaimedBy(messageId, claimId).ConfigureAwait(false);
    }

    Task Barrier()
    {
        var barrier = getBarrier(threadId, starting);
        starting = false;
        return barrier;
    }
}