using System;
using System.Collections.Generic;

namespace IntegrationEvents;

public interface IEventBusSubscriptionsManager
{
    bool IsEmpty { get; }

    event EventHandler<string> OnEventRemoved;

    /// <summary>
    /// Adds subscriptions by type name
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    void AddSubscription(Type eventType, Type eventHandlerType);

    /// <summary>
    /// unsubscribe
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    void RemoveSubscription(Type eventType, Type eventHandlerType);

    /// <summary>
    /// Determines whether a subscription with the specified name is owned
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    bool HasSubscriptionsForEvent(string eventName);

    Type GetEventTypeByName(string eventName);

    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    void Clear();

    string GetEventName(Type eventType);
}
