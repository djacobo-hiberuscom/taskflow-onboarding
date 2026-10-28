using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_user");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(user => user.KeycloakSub)
            .HasColumnName("keycloak_sub")
            .HasMaxLength(255)
            .IsRequired();
        
        builder.HasIndex(user => user.KeycloakSub)
            .IsUnique();
    }
}