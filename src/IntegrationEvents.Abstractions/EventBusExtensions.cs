namespace IntegrationEvents;

public static class EventBusExtensions
{
    /// <summary>
    /// Subscribe to the event
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public static void Subscribe<TEvent, TEventHandler>(this IIntegrationEventBus source) where TEvent : IntegrationEvent where TEventHandler : IIntegrationEventHandler<TEvent>
    {
        source.Subscribe(typeof(TEvent), typeof(TEventHandler));
    }

    /// <summary>
    /// Simplify the subscription process
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public static void Subscribe<TEvent>(this IIntegrationEventBus source) where TEvent : IntegrationEvent
    {
        source.Subscribe(typeof(TEvent), typeof(IIntegrationEventHandler<>).MakeGenericType(typeof(TEvent)));
    }

    /// <summary>
    /// Unsubscribe from the event
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <typeparam name="TEventHandler"></typeparam>
    public static void Unsubscribe<TEvent, TEventHandler>(this IIntegrationEventBus source) where TEventHandler : IIntegrationEventHandler<TEvent> where TEvent : IntegrationEvent
    {
        source.Unsubscribe(typeof(TEvent), typeof(TEventHandler));
    }
}
