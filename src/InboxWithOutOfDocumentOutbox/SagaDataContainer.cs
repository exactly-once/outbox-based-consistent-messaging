using System.Collections.Generic;

public class SagaDataContainer : IDocument
{
    public string Id { get; set; }
    public Dictionary<string, string> OutboxState = new Dictionary<string, string>();
    public object SagaData { get; set; }
    public object VersionInfo { get; set; }
}