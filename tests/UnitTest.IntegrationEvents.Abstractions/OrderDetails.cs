namespace UnitTest.IntegrationEvents.Abstractions;

public record OrderDetails(string? OrderNumber)
{
    public string? Description { get; set; }
}
