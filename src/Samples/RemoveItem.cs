using NServiceBus;

public class RemoveItem : IMessage
{
    public string CorrelationId { get; set; }
    public string Item { get; set; }
}