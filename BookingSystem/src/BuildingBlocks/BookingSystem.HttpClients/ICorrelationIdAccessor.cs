namespace BookingSystem.HttpClients;

public interface ICorrelationIdAccessor
{
    Guid? GetCorrelationId();
}
