using System;
using System.Threading.Tasks;

class TestingOutbox : IOutboxStore
{
    Func<string, Task> barrier;
    IOutboxStore impl;

    public TestingOutbox(Func<string, Task> barrier, IOutboxStore impl)
    {
        this.barrier = barrier;
        this.impl = impl;
    }

    public async Task<OutboxState> Get(string key)
    {
        await barrier("Outbox.Get").ConfigureAwait(false);
        return await impl.Get(key).ConfigureAwait(false);
    }

    public async Task Store(string key, OutboxState outgoingMessages)
    {
        await barrier("Outbox.Store").ConfigureAwait(false);
        await impl.Store(key, outgoingMessages).ConfigureAwait(false);
    }

    public async Task Remove(string key)
    {
        await barrier("Outbox.Remove").ConfigureAwait(false);
        await impl.Remove(key).ConfigureAwait(false);
    }
}