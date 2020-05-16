using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public class CosmosDbOutbox 
{
    static readonly string EndpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
        
    static readonly string PrimaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");

    CosmosClient cosmosClient;
    Database database;
    Container container;

    string databaseId = "ExactlyOnce";
    string containerId = "Outbox";
    string partitionKeyPath = "/Id";

    public async Task Initialize()
    {
        cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

        database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

        container = await database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
    }

    public async Task<OutboxState> Get(string id)
    {
        try
        {
            return (await container.ReadItemAsync<OutboxState>(id, PartitionKey.None)).Resource;
        }
        catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task CleanMessages(string messageId)
    {
        //No-op. TODO: handle with TimeToLive on the document

        return Task.CompletedTask;
    }

    public async Task Commit(Guid transactionId)
    {
        var state = await Get(transactionId.ToString());

        state.Id = state.MessageId;

        await Store(state);
    }

    public async Task Store(OutboxState outboxState)
    {
        var response = await container.UpsertItemAsync(outboxState);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new Exception("Error storing outbox item");
        }
    }
}