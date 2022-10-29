namespace IntegrationEvents;

/// <summary>
/// The event handler for the event that is raised when a new integration event is published.
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent e);
}

/// <summary>
/// The event handler for the event that is raised when a new integration event is published.
/// </summary>
public interface IIntegrationEventHandler
{
}
