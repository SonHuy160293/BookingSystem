using BookingSystem.SharedKernel.Abstractions.Message;

namespace BookingSystem.SharedKernel.Cqrs;

public interface IPublisher
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
