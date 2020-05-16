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
        var result = await impl.Get(key).ConfigureAwait(false);
        await barrier("Outbox.Get").ConfigureAwait(false);
        return result;
    }

    public async Task Store(string key, OutboxState outgoingMessages)
    {
        await impl.Store(key, outgoingMessages).ConfigureAwait(false);
        await barrier("Outbox.Store").ConfigureAwait(false);
    }

    public async Task Remove(string key)
    {
        await impl.Remove(key).ConfigureAwait(false);
        await barrier("Outbox.Remove").ConfigureAwait(false);
    }
}