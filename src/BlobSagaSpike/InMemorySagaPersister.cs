using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

class ConcurrencyException : Exception
{
}

class InMemorySagaPersister : ISagaPersister
{
    Dictionary<string, string> storage = new Dictionary<string, string>();
    Dictionary<string, int> versionInfo = new Dictionary<string, int>();

    JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public Task<SagaDataContainer> LoadByCorrelationId(string correlationId)
    {
        lock (storage)
        {
            if (storage.TryGetValue(correlationId, out var serializedContainer))
            {
                var container = JsonConvert.DeserializeObject<SagaDataContainer>(serializedContainer, jsonSerializerSettings);
                container.VersionInfo = versionInfo[correlationId];
                return Task.FromResult(container);
            }

            return Task.FromResult<SagaDataContainer>(null);
        }
    }

    public Task StoreClaimId(SagaDataContainer sagaContainer)
    {
        return Store(sagaContainer);
    }

    public Task PersistState(SagaDataContainer sagaContainer)
    {
        return Store(sagaContainer);
    }

    public Task MarkDispatched(SagaDataContainer sagaContainer)
    {
        return Store(sagaContainer);
    }

    public Task Store(SagaDataContainer sagaContainer)
    {
        lock (storage)
        {
            if (versionInfo.TryGetValue(sagaContainer.Id, out var version))
            {
                if (sagaContainer.VersionInfo == null)
                {
                    throw new ConcurrencyException();
                }
                var expectedVersion = (int) sagaContainer.VersionInfo;
                if (version != expectedVersion)
                {
                    throw new ConcurrencyException();
                }
                storage[sagaContainer.Id] = JsonConvert.SerializeObject(sagaContainer, jsonSerializerSettings);
                versionInfo[sagaContainer.Id] = version + 1;
            }
            else
            {
                storage[sagaContainer.Id] = JsonConvert.SerializeObject(sagaContainer, jsonSerializerSettings);
                versionInfo[sagaContainer.Id] = 0;
            }
            sagaContainer.VersionInfo = versionInfo[sagaContainer.Id];
        }
        return Task.CompletedTask;
    }

    public Task Delete(SagaDataContainer sagaContainer)
    {
        lock (storage)
        {
            if (versionInfo.TryGetValue(sagaContainer.Id, out var version))
            {
                var expectedVersion = (int)sagaContainer.VersionInfo;
                if (version != expectedVersion)
                {
                    throw new ConcurrencyException();
                }

                storage.Remove(sagaContainer.Id);
                versionInfo.Remove(sagaContainer.Id);
            }
            else
            {
                throw new Exception("Object not found");
            }
        }
        return Task.CompletedTask;
    }
}