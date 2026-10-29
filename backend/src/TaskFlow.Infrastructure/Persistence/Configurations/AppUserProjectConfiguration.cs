using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class AppUserProjectConfiguration : IEntityTypeConfiguration<AppUserProject>
{
    public void Configure(EntityTypeBuilder<AppUserProject> builder)
    {
        builder.ToTable("app_user_project");

        builder.HasKey(membership => new
        {
            membership.ProjectId,
            membership.UserId
        });

        builder.Property(membership => membership.ProjectId)
            .HasColumnName("project_id");

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id");

        builder.Property(membership => membership.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(membership => new
        {
            membership.UserId,
            membership.ProjectId
        });

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(membership => membership.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
