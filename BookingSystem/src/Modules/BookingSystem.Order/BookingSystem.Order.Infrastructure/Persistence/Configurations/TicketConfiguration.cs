using BookingSystem.Order.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Order.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        BookingMapping.Configure(builder, "Tickets");
        builder.ToTable("Tickets", "ordering", table =>
        {
            table.HasCheckConstraint("CK_Tickets_Status", "[Status] IN (0,1,2)");
            table.HasCheckConstraint("CK_Tickets_Hash", "LEN([QrTokenHash]) = 64 AND [QrTokenHash] NOT LIKE '%[^0-9a-f]%'");
            table.HasCheckConstraint("CK_Tickets_State",
                "([Status] = 0 AND [UsedAt] IS NULL AND [VoidedAt] IS NULL AND [VoidReasonCode] IS NULL) OR "
                + "([Status] = 1 AND [UsedAt] IS NOT NULL AND [VoidedAt] IS NULL AND [VoidReasonCode] IS NULL) OR "
                + "([Status] = 2 AND [VoidedAt] IS NOT NULL AND [VoidReasonCode] IS NOT NULL)");
        });
        builder.HasOne(x => x.Order).WithMany(x => x.Tickets).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.OrderItem).WithMany()
            .HasForeignKey(x => new { x.OrderItemId, x.OrderId, x.ShowtimeId, x.ShowtimeSeatId })
            .HasPrincipalKey(x => new { x.Id, x.OrderId, x.ShowtimeId, x.ShowtimeSeatId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.OrderItemId).IsUnique();
        builder.HasIndex(x => x.TicketCode).IsUnique();
        builder.HasIndex(x => x.QrTokenHash).IsUnique();
        builder.HasIndex(x => new { x.ShowtimeId, x.ShowtimeSeatId }).IsUnique();
    }
}
