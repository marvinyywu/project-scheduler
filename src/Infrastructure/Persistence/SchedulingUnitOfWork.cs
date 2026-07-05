using Application.Scheduling;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class SchedulingUnitOfWork(SchedulingDbContext dbContext) : ISchedulingUnitOfWork
{
    public Task<Project?> FindProjectAsync(int projectId, CancellationToken cancellationToken = default)
        => dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    public Task<ScheduleTask?> FindTaskAsync(int taskId, CancellationToken cancellationToken = default)
        => dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

    public Task<Resource?> FindResourceAsync(int resourceId, CancellationToken cancellationToken = default)
        => dbContext.Resources.FirstOrDefaultAsync(r => r.Id == resourceId, cancellationToken);

    public Task<List<ScheduleTask>> GetTasksForProjectAsync(int projectId, CancellationToken cancellationToken = default)
        => dbContext.Tasks.Where(t => t.ProjectId == projectId).ToListAsync(cancellationToken);

    public Task<List<Dependency>> GetDependenciesForTasksAsync(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default)
        => dbContext.Dependencies
            .Where(d => taskIds.Contains(d.PredecessorId) && taskIds.Contains(d.SuccessorId))
            .ToListAsync(cancellationToken);

    public Task<List<Resource>> GetResourcesByIdsAsync(IReadOnlyCollection<int> resourceIds, CancellationToken cancellationToken = default)
        => dbContext.Resources.Where(r => resourceIds.Contains(r.Id)).ToListAsync(cancellationToken);

    public Task<List<Assignment>> GetAssignmentsForTasksAsync(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default)
        => dbContext.Assignments.Where(a => taskIds.Contains(a.TaskId)).ToListAsync(cancellationToken);

    public void AddTask(ScheduleTask task) => dbContext.Tasks.Add(task);

    public void AddDependency(Dependency dependency) => dbContext.Dependencies.Add(dependency);

    public void AddResource(Resource resource) => dbContext.Resources.Add(resource);

    public void AddAssignment(Assignment assignment) => dbContext.Assignments.Add(assignment);

    public void RemoveDependency(Dependency dependency) => dbContext.Dependencies.Remove(dependency);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
