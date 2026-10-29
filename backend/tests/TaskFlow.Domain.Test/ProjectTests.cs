using TaskFlow.Domain;

namespace TaskFlow.Domain.Test;

public class ProjectTests
{
    [Fact]
    public void Archive_ActiveProject_SetsArchiveAndUpdateTime()
    {
        var project = CreateProject();
        var originalId = project.Id;
        var originalCreatedBy = project.CreatedBy;
        var originalCreatedAt = project.CreatedAt;
        var beforeArchive = DateTimeOffset.UtcNow;

        project.Archive();

        var afterArchive = DateTimeOffset.UtcNow;
        Assert.NotNull(project.ArchivedAt);
        Assert.InRange(project.ArchivedAt.Value, beforeArchive, afterArchive);
        Assert.Equal(project.ArchivedAt.Value, project.UpdatedAt);
        Assert.Equal(TimeSpan.Zero, project.ArchivedAt.Value.Offset);
        Assert.Equal(originalId, project.Id);
        Assert.Equal(originalCreatedBy, project.CreatedBy);
        Assert.Equal(originalCreatedAt, project.CreatedAt);
        Assert.Equal("Proyecto de prueba", project.Name);
        Assert.Equal("Descripción de prueba", project.Description);
    }

    [Fact]
    public void Archive_AlreadyArchivedProject_PreservesTimestamps()
    {
        var project = CreateProject();
        project.Archive();
        var originalArchivedAt = project.ArchivedAt;
        var originalUpdatedAt = project.UpdatedAt;

        project.Archive();

        Assert.NotNull(project.ArchivedAt);
        Assert.Equal(originalArchivedAt, project.ArchivedAt);
        Assert.Equal(originalUpdatedAt, project.UpdatedAt);
    }

    [Fact]
    public void Restore_ArchivedProject_ClearsArchiveAndUpdatesTime()
    {
        var project = CreateProject();
        project.Archive();
        var originalId = project.Id;
        var originalCreatedBy = project.CreatedBy;
        var originalCreatedAt = project.CreatedAt;
        var beforeRestore = DateTimeOffset.UtcNow;

        project.Restore();

        var afterRestore = DateTimeOffset.UtcNow;
        Assert.Null(project.ArchivedAt);
        Assert.InRange(project.UpdatedAt, beforeRestore, afterRestore);
        Assert.Equal(TimeSpan.Zero, project.UpdatedAt.Offset);
        Assert.Equal(originalId, project.Id);
        Assert.Equal(originalCreatedBy, project.CreatedBy);
        Assert.Equal(originalCreatedAt, project.CreatedAt);
        Assert.Equal("Proyecto de prueba", project.Name);
        Assert.Equal("Descripción de prueba", project.Description);
    }

    [Fact]
    public void Restore_ActiveProject_PreservesUpdateTime()
    {
        var project = CreateProject();
        var originalUpdatedAt = project.UpdatedAt;

        project.Restore();

        Assert.Null(project.ArchivedAt);
        Assert.Equal(originalUpdatedAt, project.UpdatedAt);
    }

    private static Project CreateProject()
    {
        return new Project("Proyecto de prueba", "Descripción de prueba", Guid.NewGuid());
    }
}
