using NodaTime;

namespace SocialAnimal.Core.Portals;

public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) 
        where TMessage : class;
    
    Task PublishAsync<TMessage>(TMessage message, Instant scheduledTime, CancellationToken cancellationToken = default) 
        where TMessage : class;
    
    Task PublishBatchAsync<TMessage>(IEnumerable<TMessage> messages, CancellationToken cancellationToken = default) 
        where TMessage : class;
}

public interface IMessageSubscriber<TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
    Task OnErrorAsync(TMessage message, Exception exception, CancellationToken cancellationToken = default);
}

public interface IMessageDispatcher
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void RegisterSubscriber<TMessage>(IMessageSubscriber<TMessage> subscriber) where TMessage : class;
}