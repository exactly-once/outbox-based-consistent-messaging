using System;
using System.Collections.Generic;
using System.Threading.Tasks;


class InMemoryOutbox : IOutboxStore
{
    Dictionary<string, OutboxState> store = new Dictionary<string, OutboxState>();

    public async Task<OutboxState> Get(string key)
    {
        lock (store)
        {
            if (store.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }
    }

    public async Task Store(string key, OutboxState outgoingMessages)
    {
        lock (store)
        {
            if (store.ContainsKey(key))
            {
                throw new InvalidOperationException("Outbox entry already exists");
            }

            store[key] = outgoingMessages;
        }
    }

    public async Task Remove(string key)
    {
        lock (store)
        {
            store.Remove(key);
        }
    }
}