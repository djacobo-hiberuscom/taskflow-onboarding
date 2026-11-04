using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("project", table =>
        {
            table.HasCheckConstraint("ck_project_name_not_empty",
            "name ~ '[^[:space:]]'");

            table.HasCheckConstraint("ck_project_description_length",
            "description IS NULL OR char_length(description) <= 5000");
        });

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(project => project.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(project => project.ArchivedAt)
            .HasColumnName("archived_at");

        builder.Property(project => project.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(project => project.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
        
        builder.HasIndex(project => project.ArchivedAt);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(project => project.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}