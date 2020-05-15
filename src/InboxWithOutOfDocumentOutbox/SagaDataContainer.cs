using System;
using Newtonsoft.Json;
using NServiceBus.Outbox;

public class SagaDataContainer : IDocument
{
    public string Id { get; set; }
    public object VersionInfo { get; set; }
    public Guid? TransactionId { get; set; }
    public object SagaData { get; set; }
}

public class E1Document<T> where T : E1Content
{
    public string ETag { get; set; }
    public T Item { get; set; }
}

public class E1Content
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public Guid? TransactionId { get; set; }
}