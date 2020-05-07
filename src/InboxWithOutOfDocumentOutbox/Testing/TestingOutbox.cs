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

    public async Task<OutboxState> Get(string messageId)
    {
        await barrier("Outbox.Get").ConfigureAwait(false);
        return await impl.Get(messageId).ConfigureAwait(false);
    }

    public async Task CleanMessages(string messageId)
    {
        await barrier("Outbox.CleanMessages").ConfigureAwait(false);
        await impl.CleanMessages(messageId).ConfigureAwait(false);
    }

    public async Task Store(Guid transactionId, string messageId, OutboxState outgoingMessages)
    {
        await barrier("Outbox.Store").ConfigureAwait(false);
        await impl.Store(transactionId, messageId, outgoingMessages).ConfigureAwait(false);
    }

    public async Task Commit(Guid transactionId)
    {
        await barrier("Outbox.Commit").ConfigureAwait(false);
        await impl.Commit(transactionId).ConfigureAwait(false);
    }
}