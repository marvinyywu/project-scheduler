using Domain.Entities;

namespace Application.Scheduling;

public sealed class AddResourceService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<Resource> AddAsync(string name, int maxUnitsPerDay, CancellationToken cancellationToken = default)
    {
        var resource = new Resource { Name = name, MaxUnitsPerDay = maxUnitsPerDay };
        unitOfWork.AddResource(resource);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return resource;
    }
}
