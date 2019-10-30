using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;

public class BlobSagaOutboxTransaction : OutboxTransaction
{
    public string MessageId { get; }

    public void Initialize(Action<OutboxMessage> onMessagesCaptured, Func<Task> onOutboxCommit, Func<Task> onDispatched)
    {
        this.onMessagesCaptured = onMessagesCaptured;
        onCommit = onOutboxCommit;
        this.onDispatched = onDispatched;
    }

    public void StoreOutgoingOperations(OutboxMessage outboxMessage)
    {
        onMessagesCaptured(outboxMessage);
    }

    public Task OnDispatched()
    {
        return onDispatched();
    }

    public Task Commit()
    {
        return onCommit();
    }

    public void Dispose()
    {
        //NOOP
    }

    public BlobSagaOutboxTransaction(string messageId)
    {
        MessageId = messageId;
    }

    Action<OutboxMessage> onMessagesCaptured;
    Func<Task> onCommit;
    Func<Task> onDispatched;
}

public class OutboxPersister : IOutboxStorage
{
    public Task<OutboxMessage> Get(string messageId, ContextBag context)
    {
        var outboxTransaction = new BlobSagaOutboxTransaction(messageId);
        context.Set(outboxTransaction);

        //Always succeed and pretend we haven't seen this message before
        return Task.FromResult<OutboxMessage>(null);
    }

    public Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        var outboxTransaction = context.Get<BlobSagaOutboxTransaction>();

        return Task.FromResult<OutboxTransaction>(outboxTransaction);
    }

    public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
    {
        var outboxTransaction = (BlobSagaOutboxTransaction) transaction;
        outboxTransaction.StoreOutgoingOperations(message);

        return Task.CompletedTask;
    }

    public Task SetAsDispatched(string messageId, ContextBag context)
    {
        var outboxTransaction = context.Get<BlobSagaOutboxTransaction>();
        return outboxTransaction.OnDispatched();
    }
}