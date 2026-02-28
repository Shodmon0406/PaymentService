using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities.Users;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        
        builder.HasKey(rt => rt.Id);
        
        builder.Property(rt => rt.Id)
            .HasColumnName("id");
        
        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id");
        
        builder.Property(rt => rt.Token)
            .HasColumnName("token")
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at");
        
        builder.Property(rt => rt.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45)
            .IsRequired();
        
        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");
        
        builder.Property(rt => rt.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(45);
        
        builder.Property(rt => rt.ReplacedByToken)
            .HasColumnName("replaced_by_token")
            .HasMaxLength(500);
        
        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at");
        
        builder.Property(rt => rt.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasIndex(rt => rt.Token).IsUnique();
    }
}