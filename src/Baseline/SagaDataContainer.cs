using System.Collections.Generic;

public class SagaDataContainer : IDocument
{
    public string Id { get; set; }
    public Dictionary<string, OutboxState> OutboxState = new Dictionary<string, OutboxState>();
    public object SagaData { get; set; }
    public object VersionInfo { get; set; }
}