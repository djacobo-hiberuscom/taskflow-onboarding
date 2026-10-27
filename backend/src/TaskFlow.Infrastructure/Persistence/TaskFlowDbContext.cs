using Microsoft.EntityFrameworkCore;
namespace TaskFlow.Infrastructure.Persistence;


public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options): base(options)
    {
        
    }
}