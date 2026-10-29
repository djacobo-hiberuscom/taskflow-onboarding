using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain;
using DomainTask = TaskFlow.Domain.Task;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<DomainTask>
{
    public void Configure(EntityTypeBuilder<DomainTask> builder)
    {
        builder.ToTable("task", table =>
        {
            table.HasCheckConstraint(
                "ck_task_title_not_empty",
                "title ~ '[^[:space:]]'");

            table.HasCheckConstraint(
                "ck_task_description_length",
                "description IS NULL OR char_length(description) <= 5000");

            table.HasCheckConstraint(
                "ck_task_status",
                "status IN ('ToDo', 'InProgress', 'Done')");

            table.HasCheckConstraint(
                "ck_task_priority",
                "priority IN ('Low', 'Medium', 'High')");
        });

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(task => task.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(task => task.AssignedId)
            .HasColumnName("assigned_id")
            .IsRequired(false);

        builder.Property(task => task.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(TaskState.ToDo)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(task => task.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(task => new { task.ProjectId, task.AssignedId });
        builder.HasIndex(task => task.AssignedId);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // La migración inicial debe reemplazar esta FK con SQL que añada
        // ON DELETE SET NULL (assigned_id). SetNull normal anularía también
        // project_id; ClientNoAction evita que EF intente modificar ambos GUID.
        builder.HasOne<AppUserProject>()
            .WithMany()
            .HasForeignKey(task => new { task.ProjectId, task.AssignedId })
            .HasPrincipalKey(membership => new { membership.ProjectId, membership.UserId })
            .OnDelete(DeleteBehavior.ClientNoAction)
            .HasConstraintName("fk_task_app_user_project_assignment");
    }
}
