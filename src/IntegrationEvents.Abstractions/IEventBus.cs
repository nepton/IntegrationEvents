namespace IntegrationEvents;

/// <summary>
/// Interface to the event bus
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes the specified event.
    /// </summary>
    /// <param name="e"></param>
    void Publish(IntegrationEvent e);

    /// <summary>
    /// Adds subscriptions by type name
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    void Subscribe(Type eventType, Type eventHandlerType);

    /// <summary>
    /// Unsubscribe from an event
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    void Unsubscribe(Type eventType, Type eventHandlerType);
}