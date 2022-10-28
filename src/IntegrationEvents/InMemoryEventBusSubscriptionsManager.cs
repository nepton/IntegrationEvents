using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationEvents;

public partial class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

    private readonly List<Type> _eventTypes;

    public event EventHandler<string> OnEventRemoved;

    public InMemoryEventBusSubscriptionsManager()
    {
        _handlers   = new Dictionary<string, List<SubscriptionInfo>>();
        _eventTypes = new List<Type>();
    }

    public bool IsEmpty => _handlers is {Count: 0};
    public void Clear() => _handlers.Clear();

    public void AddSubscription(Type eventType, Type eventHandlerType)
    {
        var eventName = GetEventName(eventType);

        AddSubscriptionCore(eventHandlerType, eventName);

        if (!_eventTypes.Contains(eventType))
        {
            _eventTypes.Add(eventType);
        }
    }

    private void AddSubscriptionCore(Type handlerType, string eventName)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<SubscriptionInfo>());
        }

        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{eventName}'",
                nameof(handlerType));
        }

        _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    public void RemoveSubscription(Type eventType, Type eventHandlerType)
    {
        var eventName       = GetEventName(eventType);
        var handlerToRemove = FindSubscriptionToRemove(eventName, eventHandlerType);

        RemoveSubscriptionCore(eventName, handlerToRemove);
    }

    private void RemoveSubscriptionCore(string eventName, SubscriptionInfo subsToRemove)
    {
        if (subsToRemove != null)
        {
            _handlers[eventName].Remove(subsToRemove);
            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                if (eventType != null)
                {
                    _eventTypes.Remove(eventType);
                }

                RaiseOnEventRemoved(eventName);
            }
        }
    }

    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

    private SubscriptionInfo FindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
    }

    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);

    /// <summary>
    /// Added a method to obtain the event name by event type
    /// </summary>
    /// <param name="eventType"></param>
    /// <returns></returns>
    public string GetEventName(Type eventType)
    {
        return eventType.Name;
    }
}
