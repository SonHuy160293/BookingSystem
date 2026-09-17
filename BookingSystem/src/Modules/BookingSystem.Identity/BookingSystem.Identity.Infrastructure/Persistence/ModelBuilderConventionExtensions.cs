using System.Linq.Expressions;
using BookingSystem.SharedKernel.Abstractions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingSystem.Identity.Infrastructure.Persistence;

internal static class ModelBuilderConventionExtensions
{
    private const int AuditUserMaxLength = 150;

    public static ModelBuilder ApplyApplicationConventions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(IAuditableEntity).IsAssignableFrom(clrType))
            {
                ConfigureAuditableEntity(modelBuilder.Entity(clrType));
            }

            if (typeof(ISoftDeletableEntity).IsAssignableFrom(clrType))
            {
                ConfigureSoftDeletableEntity(modelBuilder.Entity(clrType), clrType);
            }
        }

        return modelBuilder;
    }

    private static void ConfigureAuditableEntity(EntityTypeBuilder builder)
    {
        builder.Property(nameof(IAuditableEntity.CreatedAt)).IsRequired();
        builder.Property(nameof(IAuditableEntity.CreatedBy)).HasMaxLength(AuditUserMaxLength);
        builder.Property(nameof(IAuditableEntity.UpdatedAt));
        builder.Property(nameof(IAuditableEntity.UpdatedBy)).HasMaxLength(AuditUserMaxLength);
    }

    private static void ConfigureSoftDeletableEntity(EntityTypeBuilder builder, Type clrType)
    {
        builder.Property(nameof(ISoftDeletableEntity.IsDeleted)).IsRequired();
        builder.Property(nameof(ISoftDeletableEntity.DeletedAt));
        builder.Property(nameof(ISoftDeletableEntity.DeletedBy)).HasMaxLength(AuditUserMaxLength);
        builder.HasQueryFilter(CreateSoftDeleteFilter(clrType));
    }

    private static LambdaExpression CreateSoftDeleteFilter(Type clrType)
    {
        var parameter = Expression.Parameter(clrType, "entity");
        var property = Expression.Property(parameter, nameof(ISoftDeletableEntity.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));

        return Expression.Lambda(condition, parameter);
    }
}
