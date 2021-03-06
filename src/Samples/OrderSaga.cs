﻿using System.Threading.Tasks;
using NServiceBus;

public class OrderSaga : IHandleMessages<AddItem>,
    IHandleMessages<RemoveItem>,
    IHandleMessages<Submit>
{
    ISagaManager sagaManager;

    public OrderSaga(ISagaManager sagaManager)
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
            await context.Publish(new OrderSubmitted
            {
                CorrelationId = message.CorrelationId,
                Items = x.Items
            });
            return null; //Completes the saga
        });
    }
}