using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed record ProjectResponse(int Id, string Name, DateOnly StartDate)
{
    public static ProjectResponse FromEntity(Project project) => new(project.Id, project.Name, project.StartDate);
}
