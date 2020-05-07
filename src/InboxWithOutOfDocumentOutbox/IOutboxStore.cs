using System;
using System.Threading.Tasks;

public interface IOutboxStore
{
    Task<OutboxState> Get(string messageId);
    Task CleanMessages(string messageId);

    Task Store(Guid transactionId, string messageId, OutboxState outgoingMessages);
    Task Commit(Guid transactionId);
}