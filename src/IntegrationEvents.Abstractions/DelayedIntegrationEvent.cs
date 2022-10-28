namespace IntegrationEvents;

/// <summary>
/// Delayed integration event
/// NOTE, not all implementations support this property
/// </summary>
public abstract class DelayedIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// If the message needs to be delayed, the time in seconds that the property saves the delayed message
    /// </summary>
    public uint DelayInSec { get; protected set; }
}
