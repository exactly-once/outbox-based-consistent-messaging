using System.Collections.Generic;

public class SagaDataContainer : IDocument
{
    public string Id { get; set; }
    public string TransactionId { get; set; }
    public object SagaData { get; set; }
    public object VersionInfo { get; set; }
}