namespace IntegrationEvents;

/// <summary>
/// The base class for all integration events.
/// </summary>
public record IntegrationEvent
{
    protected IntegrationEvent()
    {
        Id          = Guid.NewGuid();
        CreatedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Message Id
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The time when the message was created
    /// </summary>
    public DateTime CreatedTime { get; init; }
}
