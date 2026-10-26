using System.Data.Common;

namespace TaskFlow.Domain;

public class Project
{
    private Guid Id { get; set; }
    private string Name { get; set; }
    private string? Description { get; set; }
    private Guid CreatedBy { get; set; }
    private DateTimeOffset ArchivedAt { get; set; }
    private DateTimeOffset CreatedAt { get; set; }
    private DateTimeOffset UpdatedAt  { get; set; }
}
