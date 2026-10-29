using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence.Configurations;
using TaskFlow.Application.Abstractions;
namespace TaskFlow.Infrastructure.Persistence;


public class TaskFlowDbContext : DbContext, IUnitOfWork
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AppUserProject> ProjectMemberships => Set<AppUserProject>();
    public DbSet<TaskFlow.Domain.Task> Tasks => Set<TaskFlow.Domain.Task>();

    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new AppUserProjectConfiguration());
        modelBuilder.ApplyConfiguration(new TaskConfiguration());
    }
}
