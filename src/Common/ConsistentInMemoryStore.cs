using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ConsistentInMemoryStore<T>
    where T: class, IDocument
{
    Dictionary<string, string> storage = new Dictionary<string, string>();
    Dictionary<string, int> versionInfo = new Dictionary<string, int>();

    JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public Task<T> Get(string id)
    {
        lock (storage)
        {
            if (storage.TryGetValue(id, out var serializedContainer))
            {
                var container = JsonConvert.DeserializeObject<T>(serializedContainer, jsonSerializerSettings);
                container.VersionInfo = versionInfo[id];
                return Task.FromResult(container);
            }

            return Task.FromResult<T>(null);
        }
    }

    public Task Delete(T doc)
    {
        lock (storage)
        {
            if (versionInfo.TryGetValue(doc.Id, out var version))
            {
                if (doc.VersionInfo == null)
                {
                    throw new ConcurrencyException();
                }
                var expectedVersion = (int)doc.VersionInfo;
                if (version != expectedVersion)
                {
                    throw new ConcurrencyException();
                }

                storage.Remove(doc.Id);
                versionInfo.Remove(doc.Id);
            }
            else
            {
                throw new InvalidOperationException("Object does not exist");
            }
        }
        return Task.CompletedTask;
    }

    public Task Put(T doc)
    {
        lock (storage)
        {
            if (versionInfo.TryGetValue(doc.Id, out var version))
            {
                if (doc.VersionInfo == null)
                {
                    throw new ConcurrencyException();
                }
                var expectedVersion = (int) doc.VersionInfo;
                if (version != expectedVersion)
                {
                    throw new ConcurrencyException();
                }
                storage[doc.Id] = JsonConvert.SerializeObject(doc, jsonSerializerSettings);
                versionInfo[doc.Id] = version + 1;
            }
            else
            {
                storage[doc.Id] = JsonConvert.SerializeObject(doc, jsonSerializerSettings);
                versionInfo[doc.Id] = 0;
            }
            doc.VersionInfo = versionInfo[doc.Id];
        }
        return Task.CompletedTask;
    }
}