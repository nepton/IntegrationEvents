using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace UnitTest.IntegrationEvents.Abstractions;

public class IntegrationEventTester
{
    [Fact]
    public void TestNewtonsoftSerialization()
    {
        // arrange
        var expected   = new OrderPlacedEvent(Guid.NewGuid(), new OrderDetails(Guid.NewGuid().ToString()));
        var serialized = JsonConvert.SerializeObject(expected);

        // act
        var actual = JsonConvert.DeserializeObject<OrderPlacedEvent>(serialized);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestSystemJsonSerialization()
    {
        // arrange
        var expected   = new OrderPlacedEvent(Guid.NewGuid(), new OrderDetails(Guid.NewGuid().ToString()));
        var serialized = JsonSerializer.Serialize(expected);

        // act
        var actual = JsonSerializer.Deserialize<OrderPlacedEvent>(serialized);

        // assert
        Assert.Equal(expected, actual);
    }
}
