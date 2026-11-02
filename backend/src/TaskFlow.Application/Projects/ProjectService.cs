using System.Data;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain;

namespace TaskFlow.Application.Projects;

public class ProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;


    public ProjectService(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Project> CreateProjectAsync(string name, string? description,
        Guid createdBy, CancellationToken cancellationToken = default)
    {
        var project = new Project(name, description, createdBy);
        _projectRepository.Add(project);

        var appUserProject = new AppUserProject(project.Id, createdBy);
        _projectRepository.AddMembership(appUserProject);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return project;
    }

    public async Task<ProjectPage> GetProjectsAsync(
        Guid userId, bool isAdmin, bool archived, int page, int pageSize, CancellationToken cancellationToken)
    {
        Guid? memberUserId = userId;
        if (isAdmin)
        {
            memberUserId = null;
        }

        var projectPage = await _projectRepository.GetPageAsync(
            memberUserId, archived, page, pageSize, cancellationToken);

        return projectPage;
    }

    public async Task<Project?> GetProjectByIdAsync(Guid projectId, Guid userId,
        bool isAdmin, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetVisibleByIdAsync(projectId, userId, isAdmin, cancellationToken);

        return project;
    }

    public async Task<AppUser[]> GetMembersAsync(
        Guid projectId, CancellationToken cancellationToken)
    {
        var members = await _projectRepository.GetMembersAsync(
            projectId, cancellationToken);
        
        return members;
    }

}