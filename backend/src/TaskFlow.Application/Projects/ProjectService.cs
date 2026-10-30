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

    public async Task<Guid> CreateProjectAsync(string name, string? description,
        Guid createdBy, CancellationToken cancellationToken = default)
    {
        var project = new Project(name, description, createdBy);
        _projectRepository.Add(project);

        var appUserProject = new AppUserProject(project.Id, createdBy);
        _projectRepository.AddMembership(appUserProject);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return project.Id;
    }

}