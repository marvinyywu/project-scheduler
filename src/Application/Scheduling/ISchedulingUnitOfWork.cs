using Domain.Entities;

namespace Application.Scheduling;

public interface ISchedulingUnitOfWork
{
    Task<Project?> FindProjectAsync(int projectId, CancellationToken cancellationToken = default);

    Task<ScheduleTask?> FindTaskAsync(int taskId, CancellationToken cancellationToken = default);

    Task<Resource?> FindResourceAsync(int resourceId, CancellationToken cancellationToken = default);

    Task<List<ScheduleTask>> GetTasksForProjectAsync(int projectId, CancellationToken cancellationToken = default);

    Task<List<Dependency>> GetDependenciesForTasksAsync(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default);

    Task<List<Resource>> GetResourcesByIdsAsync(IReadOnlyCollection<int> resourceIds, CancellationToken cancellationToken = default);

    Task<List<Assignment>> GetAssignmentsForTasksAsync(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default);

    void AddTask(ScheduleTask task);

    void AddDependency(Dependency dependency);

    void RemoveDependency(Dependency dependency);

    void AddResource(Resource resource);

    void AddAssignment(Assignment assignment);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
