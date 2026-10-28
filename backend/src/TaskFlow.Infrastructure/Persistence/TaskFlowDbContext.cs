using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain;
namespace TaskFlow.Infrastructure.Persistence;


public class TaskFlowDbContext : DbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AppUserProject> ProjectMemberships => Set<AppUserProject>();
    public DbSet<TaskFlow.Domain.Task> Tasks => Set<TaskFlow.Domain.Task>();

    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
        
    }
}