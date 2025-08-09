using System.Collections.Concurrent;
using NodaTime;
using SocialAnimal.Core.Portals;

namespace SocialAnimal.Infrastructure.Portals;

public class InMemoryMessagePortal : IMessagePublisher, IMessageDispatcher
{
    private readonly ConcurrentDictionary<Type, List<object>> _subscribers;
    private readonly ConcurrentQueue<QueuedMessage> _messageQueue;
    private readonly IClock _clock;
    private readonly ILoggerPortal _logger;
    private CancellationTokenSource? _processingCts;
    private Task? _processingTask;
    
    public InMemoryMessagePortal(IClock clock, ILoggerPortal logger)
    {
        _subscribers = new ConcurrentDictionary<Type, List<object>>();
        _messageQueue = new ConcurrentQueue<QueuedMessage>();
        _clock = clock;
        _logger = logger;
    }
    
    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        await PublishAsync(message, _clock.GetCurrentInstant(), cancellationToken);
    }
    
    public async Task PublishAsync<TMessage>(TMessage message, Instant scheduledTime, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        var queuedMessage = new QueuedMessage
        {
            Message = message,
            MessageType = typeof(TMessage),
            ScheduledTime = scheduledTime,
            EnqueuedTime = _clock.GetCurrentInstant()
        };
        
        _messageQueue.Enqueue(queuedMessage);
        
        _logger.LogDebug("Message enqueued: {MessageType} scheduled for {ScheduledTime}", 
            typeof(TMessage).Name, scheduledTime);
        
        await Task.CompletedTask;
    }
    
    public async Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default) 
        where TMessage : class
    {
        foreach (var message in messages)
        {
            await PublishAsync(message, cancellationToken);
        }
    }
    
    public void RegisterSubscriber<TMessage>(IMessageSubscriber<TMessage> subscriber) where TMessage : class
    {
        var messageType = typeof(TMessage);
        _subscribers.AddOrUpdate(messageType,
            new List<object> { subscriber },
            (_, list) =>
            {
                list.Add(subscriber);
                return list;
            });
        
        _logger.LogInformation("Subscriber registered for message type: {MessageType}", messageType.Name);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = ProcessMessagesAsync(_processingCts.Token);
        
        _logger.LogInformation("Message dispatcher started");
        await Task.CompletedTask;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _processingCts?.Cancel();
        
        if (_processingTask != null)
        {
            await _processingTask;
        }
        
        _logger.LogInformation("Message dispatcher stopped");
    }
    
    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_messageQueue.TryDequeue(out var queuedMessage))
                {
                    var now = _clock.GetCurrentInstant();
                    
                    if (queuedMessage.ScheduledTime <= now)
                    {
                        await ProcessMessage(queuedMessage, cancellationToken);
                    }
                    else
                    {
                        // Re-queue for later processing
                        _messageQueue.Enqueue(queuedMessage);
                        await Task.Delay(100, cancellationToken);
                    }
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message queue");
            }
        }
    }
    
    private async Task ProcessMessage(QueuedMessage queuedMessage, CancellationToken cancellationToken)
    {
        if (_subscribers.TryGetValue(queuedMessage.MessageType, out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    var handleMethod = subscriber.GetType()
                        .GetMethod("HandleAsync", new[] { queuedMessage.MessageType, typeof(CancellationToken) });
                    
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(subscriber, new[] { queuedMessage.Message, cancellationToken })!;
                        await task;
                    }
                    
                    _logger.LogDebug("Message processed: {MessageType}", queuedMessage.MessageType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {MessageType}", queuedMessage.MessageType.Name);
                    
                    var errorMethod = subscriber.GetType()
                        .GetMethod("OnErrorAsync", new[] { queuedMessage.MessageType, typeof(Exception), typeof(CancellationToken) });
                    
                    if (errorMethod != null)
                    {
                        var task = (Task)errorMethod.Invoke(subscriber, new[] { queuedMessage.Message, ex, cancellationToken })!;
                        await task;
                    }
                }
            }
        }
    }
    
    private class QueuedMessage
    {
        public required object Message { get; init; }
        public required Type MessageType { get; init; }
        public required Instant ScheduledTime { get; init; }
        public required Instant EnqueuedTime { get; init; }
    }
}