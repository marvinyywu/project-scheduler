using System.Text.Json;
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

    [Fact]
    public async Task CaptureBaseline_KeepsOriginalDates_AfterScheduleDrifts()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        var unitOfWork = new SchedulingUnitOfWork(db);
        var addTask = new AddTaskService(unitOfWork);
        var recompute = new RecomputeScheduleService(unitOfWork);
        var captureBaseline = new CaptureBaselineService(unitOfWork);

        db.Projects.Add(new Project { Name = "Baseline Test", StartDate = DateOnly.FromDateTime(DateTime.Today) });
        await db.SaveChangesAsync();
        var projectId = db.Projects.Single().Id;

        var a = (await addTask.AddAsync(projectId, "A", 3)).Task!;
        await recompute.RecomputeAsync(projectId);

        var captureResult = await captureBaseline.CaptureAsync(projectId);
        Assert.Equal(CaptureBaselineOutcome.Success, captureResult.Outcome);

        var trackedTask = await db.Tasks.SingleAsync(t => t.Id == a.Id);
        db.Entry(trackedTask).Property(t => t.Duration).CurrentValue = 8;
        await db.SaveChangesAsync();
        await recompute.RecomputeAsync(projectId);

        var driftedTask = await db.Tasks.SingleAsync(t => t.Id == a.Id);
        Assert.Equal(8, driftedTask.EarlyFinish);

        var baseline = await unitOfWork.FindLatestBaselineAsync(projectId);
        var snapshot = JsonSerializer.Deserialize<List<BaselineTaskSnapshot>>(baseline!.SnapshotJson)!;
        var baselineTask = snapshot.Single(s => s.TaskId == a.Id);

        Assert.Equal(0, baselineTask.EarlyStart);
        Assert.Equal(3, baselineTask.EarlyFinish);
    }

    [Fact]
    public async Task ComputeEvm_MatchesHandWorkedExample_AfterPersistenceRoundTrip()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateContext(databaseName);
        var unitOfWork = new SchedulingUnitOfWork(db);
        var addTask = new AddTaskService(unitOfWork);
        var addDependency = new AddDependencyService(unitOfWork);
        var recompute = new RecomputeScheduleService(unitOfWork);
        var updateProgress = new UpdateTaskProgressService(unitOfWork);

        db.Projects.Add(new Project { Name = "EVM Worked Example", StartDate = DateOnly.FromDateTime(DateTime.Today) });
        await db.SaveChangesAsync();
        var projectId = db.Projects.Single().Id;

        var a = (await addTask.AddAsync(projectId, "A", 3, budget: 300m)).Task!;
        var b = (await addTask.AddAsync(projectId, "B", 4, budget: 400m)).Task!;
        var c = (await addTask.AddAsync(projectId, "C", 2, budget: 200m)).Task!;
        var d = (await addTask.AddAsync(projectId, "D", 5, budget: 500m)).Task!;

        await addDependency.AddAsync(a.Id, b.Id, DependencyType.FinishToStart, 0);
        await addDependency.AddAsync(a.Id, c.Id, DependencyType.FinishToStart, 0);
        await addDependency.AddAsync(b.Id, d.Id, DependencyType.FinishToStart, 0);
        await addDependency.AddAsync(c.Id, d.Id, DependencyType.FinishToStart, 0);
        await recompute.RecomputeAsync(projectId);

        await updateProgress.UpdateAsync(a.Id, percentComplete: 100, actualCost: 320m);
        await updateProgress.UpdateAsync(b.Id, percentComplete: 60, actualCost: 250m);
        await updateProgress.UpdateAsync(c.Id, percentComplete: 100, actualCost: 180m);
        await updateProgress.UpdateAsync(d.Id, percentComplete: 0, actualCost: 0m);

        // Fresh context + fresh services, so these numbers come from what's
        // actually in the backing store, not from entities already tracked
        // in-memory by the services above.
        await using var verify = CreateContext(databaseName);
        var computeEvm = new ComputeEvmService(new SchedulingUnitOfWork(verify));

        var result = await computeEvm.ComputeAsync(projectId, asOfDay: 5);

        Assert.Equal(ComputeEvmOutcome.Success, result.Outcome);
        Assert.Equal(1400m, result.Report.Bac);
        Assert.Equal(700m, result.Report.Pv);
        Assert.Equal(740m, result.Report.Ev);
        Assert.Equal(750m, result.Report.Ac);
        Assert.Equal(1418.92, (double)result.Report.Eac, 2);
    }
}
