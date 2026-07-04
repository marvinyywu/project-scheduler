using Application.Scheduling;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests;

public class SchedulingServicesTests
{
    private static SchedulingDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new SchedulingDbContext(options);
    }

    [Fact]
    public async Task AddTask_ReturnsProjectNotFound_WhenProjectDoesNotExist()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var service = new AddTaskService(new SchedulingUnitOfWork(db));

        var result = await service.AddAsync(projectId: 999, name: "A", duration: 3);

        Assert.Equal(AddTaskOutcome.ProjectNotFound, result.Outcome);
    }

    [Fact]
    public async Task RecomputeSchedule_MatchesWorkedExample_AfterAddingTasksAndDependencies()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        var unitOfWork = new SchedulingUnitOfWork(db);
        var addTask = new AddTaskService(unitOfWork);
        var addDependency = new AddDependencyService(unitOfWork);
        var recompute = new RecomputeScheduleService(unitOfWork);

        db.Projects.Add(new Project { Name = "Worked Example", StartDate = DateOnly.FromDateTime(DateTime.Today) });
        await db.SaveChangesAsync();
        var projectId = db.Projects.Single().Id;

        var a = (await addTask.AddAsync(projectId, "A", 3)).Task!;
        var b = (await addTask.AddAsync(projectId, "B", 4)).Task!;
        var c = (await addTask.AddAsync(projectId, "C", 2)).Task!;
        var d = (await addTask.AddAsync(projectId, "D", 5)).Task!;

        Assert.Equal(AddDependencyOutcome.Created, (await addDependency.AddAsync(a.Id, b.Id, DependencyType.FinishToStart, 0)).Outcome);
        Assert.Equal(AddDependencyOutcome.Created, (await addDependency.AddAsync(a.Id, c.Id, DependencyType.FinishToStart, 0)).Outcome);
        Assert.Equal(AddDependencyOutcome.Created, (await addDependency.AddAsync(b.Id, d.Id, DependencyType.FinishToStart, 0)).Outcome);
        Assert.Equal(AddDependencyOutcome.Created, (await addDependency.AddAsync(c.Id, d.Id, DependencyType.FinishToStart, 0)).Outcome);

        var result = await recompute.RecomputeAsync(projectId);

        Assert.Equal(RecomputeOutcome.Success, result.Outcome);
        Assert.Equal(12, result.ProjectDuration);

        // Re-fetch from a fresh context (same backing store) to prove the computed
        // fields were actually persisted, not just mutated on in-memory instances.
        await using var verify = CreateContext(databaseName);

        var persistedD = await verify.Tasks.SingleAsync(t => t.Id == d.Id);
        Assert.Equal(7, persistedD.EarlyStart);
        Assert.Equal(12, persistedD.EarlyFinish);
        Assert.True(persistedD.IsCritical);

        var persistedC = await verify.Tasks.SingleAsync(t => t.Id == c.Id);
        Assert.Equal(2, persistedC.TotalFloat);
        Assert.False(persistedC.IsCritical);
    }

    [Fact]
    public async Task AddDependency_RejectsCycle_AndPersistsNothing()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var unitOfWork = new SchedulingUnitOfWork(db);
        var addTask = new AddTaskService(unitOfWork);
        var addDependency = new AddDependencyService(unitOfWork);

        db.Projects.Add(new Project { Name = "Cycle Test", StartDate = DateOnly.FromDateTime(DateTime.Today) });
        await db.SaveChangesAsync();
        var projectId = db.Projects.Single().Id;

        var a = (await addTask.AddAsync(projectId, "A", 1)).Task!;
        var b = (await addTask.AddAsync(projectId, "B", 1)).Task!;
        var c = (await addTask.AddAsync(projectId, "C", 1)).Task!;

        Assert.Equal(AddDependencyOutcome.Created, (await addDependency.AddAsync(a.Id, b.Id, DependencyType.FinishToStart, 0)).Outcome);
        Assert.Equal(AddDependencyOutcome.Created, (await addDependency.AddAsync(b.Id, c.Id, DependencyType.FinishToStart, 0)).Outcome);

        var cyclic = await addDependency.AddAsync(c.Id, a.Id, DependencyType.FinishToStart, 0);

        Assert.Equal(AddDependencyOutcome.CycleDetected, cyclic.Outcome);
        Assert.Equal(2, db.Dependencies.Count());
    }
}
