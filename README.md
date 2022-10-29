# IntegrationEvents

[![Build status](https://ci.appveyor.com/api/projects/status/rdai4o302paf94fe?svg=true)](https://ci.appveyor.com/project/nepton/integrationevents)
[![CodeQL](https://github.com/nepton/IntegrationEvents/actions/workflows/codeql.yml/badge.svg)](https://github.com/nepton/IntegrationEvents/actions/workflows/codeql.yml)
![GitHub issues](https://img.shields.io/github/issues/nepton/IntegrationEvents.svg)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/nepton/IntegrationEvents/blob/master/LICENSE)

IntegrationEvents is a library that provides a simple way to implement
the [Integration Events](https://microservices.io/patterns/data/transactional-outbox.html) pattern.

## Nuget packages

| Name                       | Version                                                                                                                               | Downloads                                                                                                                              |
|----------------------------|---------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| IntegrationEvents          | [![nuget](https://img.shields.io/nuget/v/IntegrationEvents.svg)](https://www.nuget.org/packages/IntegrationEvents/)                   | [![stats](https://img.shields.io/nuget/dt/IntegrationEvents.svg)](https://www.nuget.org/packages/IntegrationEvents/)                   |
| IntegrationEvents.RabbitMq | [![nuget](https://img.shields.io/nuget/v/IntegrationEvents.RabbitMq.svg)](https://www.nuget.org/packages/IntegrationEvents.RabbitMq/) | [![stats](https://img.shields.io/nuget/dt/IntegrationEvents.RabbitMq.svg)](https://www.nuget.org/packages/IntegrationEvents.RabbitMq/) |

## Installation

Add following nuget reference in business project:

```
PM> Install-Package IntegrationEvents
```

And add following nuget reference in integration project:

``` 
PM> Install-Package IntegrationEvents.RabbitMq
```

## How to use

First of all, you need to create a class that derived from the `IntegrationEvent` class.

```csharp
public class OrderCreatedIntegrationEvent : IntegrationEvent
{
    public OrderCreatedIntegrationEvent(Guid orderId, string orderName)
    {
        OrderId = orderId;
        OrderName = orderName;
    }

    public Guid OrderId { get; }

    public string OrderName { get; }
}
```

Then, you need to create a class that derived from the `IntegrationEventHandler` class.

```csharp
public class OrderCreatedIntegrationEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public override Task Handle(OrderCreatedIntegrationEvent @event)
    {
    }
}
```

Finally, you need to register the `IntegrationEventHandler` class in the `IServiceCollection`.
```csharp
services.AddIntegrationEventBus(builder =>
{
    builder.AddIntegrationEventsWithRabbitMq();
    builder.AddIntegrationEventHandler<OrderCreatedIntegrationEventHandler>();
});
```

Ok! Now you can publish the `IntegrationEvent` class in the business project.

```csharp
var integrationEventBus = serviceProvider.GetRequiredService<IIntegrationEventBus>();
await integrationEventBus.PublishAsync(new OrderCreatedIntegrationEvent(Guid.NewGuid(), "OrderName"));
``` 

## Final
Leave a comment on GitHub if you have any questions or suggestions.

Turn on the star if you like this project.

## License

This project is licensed under the MIT License
