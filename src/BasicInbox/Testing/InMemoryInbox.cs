using System.Collections.Generic;
using System.Threading.Tasks;

class InMemoryInbox : IInboxStore
{
    HashSet<string> storage = new HashSet<string>();

    public async Task<bool> HasBeenProcessed(string messageId)
    {
        lock (storage)
        {
            return storage.Contains(messageId);
        }
    }

    public async Task MarkProcessed(string messageId)
    {
        lock (storage)
        {
            storage.Add(messageId);
        }
    }
}