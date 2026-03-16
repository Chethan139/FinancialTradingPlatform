using Azure.Messaging.ServiceBus;
using EventContracts.Base;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Messaging.ServiceBus;

/// <summary>
/// Azure Service Bus publisher for reliable message delivery
/// Used for: Commands, events, and cross-service communication
/// 
/// Service Bus vs Event Hubs:
/// - Service Bus: Message queuing with guaranteed delivery, sessions, transactions
/// - Event Hubs: High-throughput event streaming, partitions, retention
/// 
/// When to use Service Bus:
/// - Commands that require guaranteed delivery
/// - Messages that need ordering (sessions)
/// - Dead letter queue for failed messages
/// - Complex routing with topics and filters
/// </summary>
public class ServiceBusPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusPublisher(ServiceBusClient client, ILogger<ServiceBusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Publish event to a topic (pub/sub pattern)
    /// Multiple subscribers can receive the same event via subscriptions
    /// </summary>
    public async Task PublishEventAsync<TEvent>(TEvent @event, string topicName) 
        where TEvent : IntegrationEvent
    {
        var sender = GetOrCreateSender(topicName);
        
        var messageBody = JsonSerializer.Serialize(@event);
        var message = new ServiceBusMessage(messageBody)
        {
            MessageId = @event.EventId.ToString(),
            CorrelationId = @event.CorrelationId,
            Subject = @event.EventType,
            ContentType = "application/json",
            ApplicationProperties =
            {
                ["EventType"] = @event.EventType,
                ["OccurredAt"] = @event.OccurredAt.ToString("o"),
                ["Version"] = @event.Version,
                ["SourceService"] = @event.SourceService
            }
        };

        // Enable duplicate detection
        message.MessageId = @event.EventId.ToString();

        await sender.SendMessageAsync(message);
        
        _logger.LogInformation(
            "Published {EventType} to topic {Topic}. EventId: {EventId}, CorrelationId: {CorrelationId}",
            @event.EventType,
            topicName,
            @event.EventId,
            @event.CorrelationId
        );
    }

    /// <summary>
    /// Send message to a queue (point-to-point pattern)
    /// Single consumer will process the message
    /// </summary>
    public async Task SendMessageAsync<TMessage>(TMessage message, string queueName)
    {
        var sender = GetOrCreateSender(queueName);
        
        var messageBody = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        await sender.SendMessageAsync(serviceBusMessage);
        
        _logger.LogInformation("Sent message to queue {Queue}", queueName);
    }

    /// <summary>
    /// Send scheduled message (future delivery)
    /// </summary>
    public async Task ScheduleMessageAsync<TMessage>(
        TMessage message,
        string queueOrTopicName,
        DateTimeOffset scheduleTime)
    {
        var sender = GetOrCreateSender(queueOrTopicName);
        
        var messageBody = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            ContentType = "application/json"
        };

        await sender.ScheduleMessageAsync(serviceBusMessage, scheduleTime);
        
        _logger.LogInformation(
            "Scheduled message to {Destination} for {ScheduleTime}",
            queueOrTopicName,
            scheduleTime
        );
    }

    private ServiceBusSender GetOrCreateSender(string queueOrTopicName)
    {
        if (!_senders.ContainsKey(queueOrTopicName))
        {
            _senders[queueOrTopicName] = _client.CreateSender(queueOrTopicName);
        }
        return _senders[queueOrTopicName];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        await _client.DisposeAsync();
    }
}
