using NServiceBus;

public class AddItem : IMessage
{
    public string CorrelationId { get; set; }
    public string Item { get; set; }
}