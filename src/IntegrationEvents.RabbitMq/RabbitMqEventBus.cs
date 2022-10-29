using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace IntegrationEvents.RabbitMq;

public class RabbitMqEventBus : IIntegrationEventBus, IDisposable
{
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly ILogger<RabbitMqEventBus>     _logger;
    private readonly IServiceProvider              _serviceProvider;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly int                           _retryCount;

    private          IModel _consumerChannel;
    private          string _queueName;
    private readonly string _brokerName;

    public RabbitMqEventBus(
        IRabbitMqPersistentConnection persistentConnection,
        ILogger<RabbitMqEventBus>     logger,
        IServiceProvider              serviceProvider,
        IEventBusSubscriptionsManager subsManager,
        string                        brokerName,
        string                        queueName  = null,
        int                           retryCount = 5)
    {
        _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
        _logger               = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider      = serviceProvider;
        _brokerName           = brokerName;
        _subsManager          = subsManager ?? new InMemoryEventBusSubscriptionsManager();
        _queueName            = queueName;
        _consumerChannel      = CreateConsumerChannel();
        _retryCount           = retryCount;

        _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
    }

    private void SubsManager_OnEventRemoved(object sender, string eventName)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        using var channel = _persistentConnection.CreateModel();
        channel.QueueUnbind(queue: _queueName,
            exchange: _brokerName,
            routingKey: eventName);

        if (_subsManager.IsEmpty)
        {
            _queueName = string.Empty;
            _consumerChannel.Close();
        }
    }

    public void Publish(IntegrationEvent e)
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", e.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

        var eventName = e.GetType().Name;

        var body = JsonSerializer.SerializeToUtf8Bytes(e,
            e.GetType(),
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        policy.Execute(() =>
        {
            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", e.Id, eventName);
            using var channel = _persistentConnection.CreateModel();

            _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", e.Id);
            channel.ExchangeDeclare(exchange: _brokerName, type: "direct");

            var properties = channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent

            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["@PublishingEvent"] = e,
                   }))
            {
                _logger.LogInformation("Publishing event {PublishingEventName} to RabbitMQ", eventName);
            }

            channel.BasicPublish(
                exchange: _brokerName,
                routingKey: eventName,
                mandatory: true,
                basicProperties: properties,
                body: body);
        });
    }

    /// <summary>
    /// 增加通过类型名称的订阅
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    public void Subscribe(Type eventType, Type eventHandlerType)
    {
        var eventName = _subsManager.GetEventName(eventType);
        DoInternalSubscription(eventName);

        _logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, eventHandlerType.GetGenericTypeName());

        _subsManager.AddSubscription(eventType, eventHandlerType);
        StartBasicConsume();
    }

    private void DoInternalSubscription(string eventName)
    {
        var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
        if (!containsKey)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            // 延迟消息处理
            _consumerChannel.QueueBind(queue: _queueName,
                exchange: _brokerName,
                routingKey: eventName);
        }
    }

    /// <summary>
    /// 取消事件订阅
    /// </summary>
    /// <param name="eventType"></param>
    /// <param name="eventHandlerType"></param>
    public void Unsubscribe(Type eventType, Type eventHandlerType)
    {
        var eventName = _subsManager.GetEventName(eventType);

        _logger.LogInformation("Unsubscribing from event {EventName}", eventName);
        _subsManager.RemoveSubscription(eventType, eventHandlerType);
    }

    private void StartBasicConsume()
    {
        _logger.LogTrace("Starting RabbitMQ basic consume");

        if (_consumerChannel != null)
        {
            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

            consumer.Received += Consumer_Received;

            _consumerChannel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);
        }
        else
        {
            _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
        }
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
    {
        var eventName = eventArgs.RoutingKey;
        var message   = Encoding.UTF8.GetString(eventArgs.Body.Span);

        try
        {
            var policy = Policy.Handle<Exception>()
                .WaitAndRetry(5,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        _logger.LogWarning(ex, "Could not handling event: {EventName} after {Timeout}s ({ExceptionMessage})", eventName, $"{time.TotalSeconds:n1}", ex.Message);
                    });

            await policy.Execute(async () => await ProcessEvent(eventName, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "----- ERROR Processing message {EventName} \"{Message}\"", eventName, message);
        }

        // 消息处理完毕, 手动确认
        _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
    }

    private IModel CreateConsumerChannel()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _logger.LogTrace("Creating RabbitMQ consumer channel");

        var channel = _persistentConnection.CreateModel();

        // 声明交换机, 否则 bind queue 的时候会 404
        channel.ExchangeDeclare(exchange: _brokerName,
            type: "direct");
        
        channel.QueueDeclare(queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.CallbackException += (_, ea) =>
        {
            _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

            _consumerChannel.Dispose();
            _consumerChannel = CreateConsumerChannel();
            StartBasicConsume();
        };

        return channel;
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

        if (!_subsManager.HasSubscriptionsForEvent(eventName))
        {
            _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            return;
        }

        var subscriptions = _subsManager.GetHandlersForEvent(eventName);
        foreach (var subscription in subscriptions)
        {
            using var scope            = _serviceProvider.CreateScope();
            var       eventType        = _subsManager.GetEventTypeByName(eventName);
            var       integrationEvent = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions() {PropertyNameCaseInsensitive = true});
            var       concreteType     = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

            // log the event
            object eventId     = integrationEvent is IntegrationEvent @event ? @event.Id : "N/A";
            var    handlerId   = Guid.NewGuid().ToString().Substring(0, 8); // 这个随机值已经可以防止重复了
            var    handlerType = subscription.HandlerType.GetGenericTypeName();

            // begin event scope
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["HandlingType"]      = handlerType,
                ["HandlingId"]        = $"{eventId}:{handlerId}",
                ["HandlingEventId"]   = eventId,
                ["HandlingEventName"] = eventName,
            });

            if (scope.ServiceProvider.GetService(subscription.HandlerType) is not { } handler)
            {
                _logger.LogWarning("No handler found for event: {EventName}", eventName);
                continue;
            }

            // 记录日志
            using (_logger.BeginScope(new Dictionary<string, object>()
                   {
                       ["@Event"] = integrationEvent
                   }))
            {
                _logger.LogInformation("Handling event {HandlingEventName} ({HandlingEventId})", eventName, eventId);
            }

            await Task.Yield();

            try
            {
                // Execute using Expression mode
                var method    = concreteType.GetMethod("Handle")!;
                var parameter = Expression.Parameter(eventType);
                var handle    = Expression.Call(Expression.Constant(handler), method, parameter);
                var func      = Expression.Lambda(handle, parameter).Compile();

                if (func.DynamicInvoke(integrationEvent) is Task task)
                    await task;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "---- Error when handling event {HandlingEventName} by {HandlingType}", eventName, handlerType);
            }
        }
    }

    public void Dispose()
    {
        if (_consumerChannel != null)
        {
            _consumerChannel.Dispose();
        }

        _subsManager.Clear();
    }
}
