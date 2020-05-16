using System.Threading.Tasks;

public interface IOutboxStore
{
    Task<OutboxState> Get(string key);
    Task Store(string key, OutboxState outgoingMessages);
    Task Remove(string key);
}