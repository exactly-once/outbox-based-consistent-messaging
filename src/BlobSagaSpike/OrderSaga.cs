using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;

public class AddItem : IMessage
{
    public string CorrelationId { get; set; }
    public string Item { get; set; }
}

public class RemoveItem : IMessage
{
    public string CorrelationId { get; set; }
    public string Item { get; set; }
}

public class Submit : IMessage
{
    public string CorrelationId { get; set; }
}

public class OrderSubmitted : IEvent
{
    public string CorrelationId { get; set; }
    public List<string> Items { get; set; }
}

public class OrderSaga : IHandleMessages<AddItem>,
    IHandleMessages<RemoveItem>,
    IHandleMessages<Submit>
{
    SagaManager sagaManager;

    public OrderSaga(SagaManager sagaManager)
    {
        this.sagaManager = sagaManager;
    }

    public Task Handle(AddItem message, IMessageHandlerContext context)
    {
        return sagaManager.Process<OrderSagaData>(message.CorrelationId, context, x =>
        {
            x.Items.Add(message.Item);
            return Task.FromResult(x);
        });
    }

    public Task Handle(RemoveItem message, IMessageHandlerContext context)
    {
        return sagaManager.Process<OrderSagaData>(message.CorrelationId, context, x =>
        {
            x.Items.Remove(message.Item);
            return Task.FromResult(x);
        });
    }

    public Task Handle(Submit message, IMessageHandlerContext context)
    {
        return sagaManager.Process<OrderSagaData>(message.CorrelationId, context, async x =>
        {
            await context.Publish(new OrderSubmitted()
            {
                CorrelationId = message.CorrelationId,
                Items = x.Items
            });
            return null; //Completes the saga
        });
    }
}

public class OrderSagaData
{
    public List<string> Items { get; set; }
}