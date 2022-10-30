using IntegrationEvents;

namespace UnitTest.IntegrationEvents.Abstractions;

public record OrderPlacedEvent(Guid OrderId, OrderDetails Details) : IntegrationEvent
{
    public void SetName(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Used for test private setter
    /// </summary>
    public string? Name { get; private set; }
}
