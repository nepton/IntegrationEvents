using System;
using RabbitMQ.Client;

namespace IntegrationEvents.RabbitMq;

public interface IRabbitMqPersistentConnection
    : IDisposable
{
    bool IsConnected { get; }

    bool TryConnect();

    IModel CreateModel();
}
