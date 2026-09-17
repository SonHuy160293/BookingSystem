namespace BookingSystem.Order.Domain.Models;

// Values are supplied by a future cryptographic credential issuer. Raw QR secrets
// are never retained in the model; QrTokenHash contains their SHA-256 hash.
public sealed record TicketCredentials(Guid OrderItemId, string TicketCode, string QrTokenHash);
