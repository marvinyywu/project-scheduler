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

    public Task<List<ScheduleTask>> GetTasksForProjectAsync(int projectId, CancellationToken cancellationToken = default)
        => dbContext.Tasks.Where(t => t.ProjectId == projectId).ToListAsync(cancellationToken);

    public Task<List<Dependency>> GetDependenciesForTasksAsync(IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default)
        => dbContext.Dependencies
            .Where(d => taskIds.Contains(d.PredecessorId) && taskIds.Contains(d.SuccessorId))
            .ToListAsync(cancellationToken);

    public void AddTask(ScheduleTask task) => dbContext.Tasks.Add(task);

    public void AddDependency(Dependency dependency) => dbContext.Dependencies.Add(dependency);

    public void RemoveDependency(Dependency dependency) => dbContext.Dependencies.Remove(dependency);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
