using Domain.Entities;
using Domain.Scheduling;
using Xunit;

namespace Domain.Tests;

public class CpmEngineTests
{
    [Fact]
    public void ComputesForwardAndBackwardPassForWorkedExample()
    {
        var a = new ScheduleTask { Id = 1, Name = "A", Duration = 3 };
        var b = new ScheduleTask { Id = 2, Name = "B", Duration = 4 };
        var c = new ScheduleTask { Id = 3, Name = "C", Duration = 2 };
        var d = new ScheduleTask { Id = 4, Name = "D", Duration = 5 };
        var tasks = new[] { a, b, c, d };

        var dependencies = new[]
        {
            new Dependency { Id = 1, PredecessorId = a.Id, SuccessorId = b.Id },
            new Dependency { Id = 2, PredecessorId = a.Id, SuccessorId = c.Id },
            new Dependency { Id = 3, PredecessorId = b.Id, SuccessorId = d.Id },
            new Dependency { Id = 4, PredecessorId = c.Id, SuccessorId = d.Id },
        };

        var projectDuration = CpmEngine.Compute(tasks, dependencies);

        Assert.Equal(12, projectDuration);

        Assert.Equal(0, a.EarlyStart);
        Assert.Equal(3, a.EarlyFinish);
        Assert.Equal(3, b.EarlyStart);
        Assert.Equal(7, b.EarlyFinish);
        Assert.Equal(3, c.EarlyStart);
        Assert.Equal(5, c.EarlyFinish);
        Assert.Equal(7, d.EarlyStart);
        Assert.Equal(12, d.EarlyFinish);

        Assert.Equal(0, a.LateStart);
        Assert.Equal(3, a.LateFinish);
        Assert.Equal(3, b.LateStart);
        Assert.Equal(7, b.LateFinish);
        Assert.Equal(5, c.LateStart);
        Assert.Equal(7, c.LateFinish);
        Assert.Equal(7, d.LateStart);
        Assert.Equal(12, d.LateFinish);

        Assert.Equal(0, a.TotalFloat);
        Assert.Equal(0, b.TotalFloat);
        Assert.Equal(2, c.TotalFloat);
        Assert.Equal(0, d.TotalFloat);

        Assert.True(a.IsCritical);
        Assert.True(b.IsCritical);
        Assert.False(c.IsCritical);
        Assert.True(d.IsCritical);
    }

    [Fact]
    public void RejectsCyclicNetwork()
    {
        var a = new ScheduleTask { Id = 1, Name = "A", Duration = 1 };
        var b = new ScheduleTask { Id = 2, Name = "B", Duration = 1 };
        var c = new ScheduleTask { Id = 3, Name = "C", Duration = 1 };
        var tasks = new[] { a, b, c };

        var dependencies = new[]
        {
            new Dependency { Id = 1, PredecessorId = a.Id, SuccessorId = b.Id },
            new Dependency { Id = 2, PredecessorId = b.Id, SuccessorId = c.Id },
            new Dependency { Id = 3, PredecessorId = c.Id, SuccessorId = a.Id },
        };

        Assert.Throws<ScheduleCycleException>(() => CpmEngine.Compute(tasks, dependencies));
    }

    [Fact]
    public void HonoursStartToStartLag()
    {
        var a = new ScheduleTask { Id = 1, Name = "A", Duration = 5 };
        var b = new ScheduleTask { Id = 2, Name = "B", Duration = 3 };
        var tasks = new[] { a, b };

        var dependencies = new[]
        {
            new Dependency
            {
                Id = 1,
                PredecessorId = a.Id,
                SuccessorId = b.Id,
                Type = DependencyType.StartToStart,
                LagDays = 2,
            },
        };

        var projectDuration = CpmEngine.Compute(tasks, dependencies);

        Assert.Equal(5, projectDuration);
        Assert.Equal(0, a.EarlyStart);
        Assert.Equal(2, b.EarlyStart);
        Assert.Equal(5, b.EarlyFinish);
        Assert.True(a.IsCritical);
        Assert.True(b.IsCritical);
    }
}
