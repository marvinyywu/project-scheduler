using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed record ResourceResponse(int Id, string Name, int MaxUnitsPerDay)
{
    public static ResourceResponse FromEntity(Resource resource) => new(resource.Id, resource.Name, resource.MaxUnitsPerDay);
}
