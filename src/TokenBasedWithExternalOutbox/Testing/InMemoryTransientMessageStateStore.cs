using System.Collections.Generic;

internal class InMemoryTransientMessageStateStore : ITransientMessageStateStore
{
    HashSet<string> storage = new HashSet<string>();

    public void MarkProcessed(string messageId)
    {
        lock (storage)
        {
            storage.Add(messageId);
        }
    }

    public bool HasBeenProcessed(string messageId)
    {
        return false;
        lock (storage)
        {
            return storage.Contains(messageId);
        }
    }
}