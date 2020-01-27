using System;
using System.Threading.Tasks;

class TestingInbox : IInboxStore
{
    Func<string, Task> barrier;
    IInboxStore impl;

    public TestingInbox(Func<string, Task> barrier, IInboxStore impl)
    {
        this.barrier = barrier;
        this.impl = impl;
    }

    public async Task<bool> HasBeenProcessed(string messageId)
    {
        await barrier("Inbox.HasBeenProcessed").ConfigureAwait(false);
        return await impl.HasBeenProcessed(messageId).ConfigureAwait(false);
    }

    public async Task MarkProcessed(string messageId)
    {
        await barrier("Inbox.MarkProcessed").ConfigureAwait(false);
        await impl.MarkProcessed(messageId).ConfigureAwait(false);
    }
}