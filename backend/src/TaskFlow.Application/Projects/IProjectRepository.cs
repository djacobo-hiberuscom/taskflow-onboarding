using TaskFlow.Domain;

namespace TaskFlow.Application.Projects;

public interface IProjectRepository
{
    void Add(Project project);

    void AddMembership(AppUserProject membership);
}

