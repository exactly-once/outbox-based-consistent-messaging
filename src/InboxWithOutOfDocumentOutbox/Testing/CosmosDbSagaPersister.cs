using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace InboxWithOutOfDocumentOutbox.Testing
{
    public class CosmosDbSagaPersister : ISagaPersister
    {
        static readonly string EndpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
        
        static readonly string PrimaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");

        CosmosClient cosmosClient;
        Database database;
        Container container;

        string databaseId = "ExactlyOnce";
        string containerId = "Sagas";
        string partitionKeyPath = "/Id";

        public async Task Initialize()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

            container = await database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
        }

        public async Task<E1Document<T>> Load<T>(string itemId) where T : E1Content
        {
            try
            {
                var response = await container.ReadItemAsync<T>(itemId, new PartitionKey(itemId));

                var result = new E1Document<T>
                {
                    ETag = response.ETag,
                    Item = response
                };

                return result;
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task Persist<T>(E1Document<T> document) where T : E1Content
        {
            var response = await container.UpsertItemAsync(
                document.Item, 
                requestOptions: new ItemRequestOptions
                {
                    IfMatchEtag = document.ETag,
                    
                });

            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new Exception("Optimistic concurrency exception on document persist");
            }

            document.ETag = response.Headers.ETag;
        }
    }
}