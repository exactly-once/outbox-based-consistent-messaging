using System.Collections.Generic;
using System.Threading.Tasks;

internal class InMemoryTokenStore : ITokenStore
{
    HashSet<string> storage = new HashSet<string>();

    public Task Delete(string messageId)
    {
        lock (storage)
        {
            storage.Remove(messageId);
        }
        return Task.CompletedTask;
    }

    public Task<bool> Exists(string id)
    {
        bool result;
        lock (storage)
        {
            result = storage.Contains(id);
        }
        return Task.FromResult(result);
    }

    public void Create(string messageId)
    {
        storage.Add(messageId);
    }
}