using System;
using System.Collections.Generic;
using System.Threading.Tasks;


class InMemoryOutbox : IOutboxStore
{
    Dictionary<Guid, (string, OutboxState)> pendingTransactions = new Dictionary<Guid, (string, OutboxState)>();
    Dictionary<string, OutboxState> committedTransactions = new Dictionary<string, OutboxState>();

    public async Task<OutboxState> Get(string messageId)
    {
        lock (committedTransactions)
        {
            if (committedTransactions.TryGetValue(messageId, out var value))
            {
                return value;
            }

            return null;
        }
    }

    public Task CleanMessages(string messageId)
    {
        lock (committedTransactions)
        {
            committedTransactions[messageId].OutgoingMessages = null;

            return Task.CompletedTask;
        }
    }

    public Task Commit(Guid transactionId)
    {
        string messageId;
        OutboxState outboxState;

        lock (pendingTransactions)
        {
            (messageId, outboxState) = pendingTransactions[transactionId];
        }

        lock (committedTransactions)
        {
            committedTransactions.Add(messageId, outboxState);
        }

        return Task.CompletedTask;
    }

    public async Task Store(Guid transactionId, string messageId, OutboxState outgoingMessages)
    {
        lock (pendingTransactions)
        {
            if (pendingTransactions.ContainsKey(transactionId))
            {
                throw new InvalidOperationException("Outbox entry already exists");
            }

            pendingTransactions[transactionId] = (messageId, outgoingMessages);
        }
    }
}