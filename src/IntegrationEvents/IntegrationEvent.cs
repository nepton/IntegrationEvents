namespace IntegrationEvents;

/// <summary>
/// The base class for all integration events.
/// </summary>
public abstract class IntegrationEvent
{
    protected IntegrationEvent()
    {
        Id          = Guid.NewGuid();
        CreatedTime = DateTime.Now;
    }

    /// <summary>
    /// Message Id
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The time when the message was created
    /// </summary>
    public DateTime CreatedTime { get; private set; }
}
