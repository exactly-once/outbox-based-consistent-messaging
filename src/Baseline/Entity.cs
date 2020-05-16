using System.Collections.Generic;

public class Entity : IDocument
{
    public string Id { get; set; }
    public Dictionary<string, OutboxState> OutboxState = new Dictionary<string, OutboxState>();
    public object BusinessState { get; set; }
    public object VersionInfo { get; set; }
}