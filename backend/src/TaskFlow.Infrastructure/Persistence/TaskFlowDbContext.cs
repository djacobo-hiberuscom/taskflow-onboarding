using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence.Configurations;
using TaskFlow.Application.Abstractions;
using System.Data;
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
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
    Func<CancellationToken, Task<TResult>> operation,
    CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var result = await operation(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return result;
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
