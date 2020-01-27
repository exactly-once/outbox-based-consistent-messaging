using NServiceBus;

public class Submit : IMessage
{
    public string CorrelationId { get; set; }
}