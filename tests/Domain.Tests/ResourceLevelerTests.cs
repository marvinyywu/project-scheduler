using Domain.Entities;
using Domain.Scheduling;
using Xunit;

namespace Domain.Tests;

public class ResourceLevelerTests
{
    [Fact]
    public void LevelsWorkedExample_DelaysProjectByTwoDays_WhenBAndCShareAConstrainedResource()
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

        var originalDuration = CpmEngine.Compute(tasks, dependencies);
        Assert.Equal(12, originalDuration);

        // B (days 3-6) and C (days 3-4) both run in parallel per CPM, and both
        // need R1's only unit of capacity - that's the over-allocation leveling
        // has to resolve.
        var resource = new Resource { Id = 1, Name = "R1", MaxUnitsPerDay = 1 };
        var assignments = new[]
        {
            new Assignment { Id = 1, TaskId = b.Id, ResourceId = resource.Id, Units = 1 },
            new Assignment { Id = 2, TaskId = c.Id, ResourceId = resource.Id, Units = 1 },
        };

        var result = ResourceLeveler.Level(tasks, dependencies, assignments, new Dictionary<int, Resource> { [resource.Id] = resource });

        // C waits for B to fully vacate R1 (day 7), then D waits for C's new
        // finish (day 9) instead of the original day 7 - a 2-day project slip.
        Assert.Equal(14, result.ProjectDuration);

        var leveledC = result.LeveledTasks.Single(t => t.TaskId == c.Id);
        Assert.Equal(7, leveledC.LeveledStart);
        Assert.Equal(4, leveledC.Delay);
    }

    [Fact]
    public void LevelsWorkedExample_NoDelay_WhenResourceCapacityIsSufficient()
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

        CpmEngine.Compute(tasks, dependencies);

        // Same shared resource, but MaxUnitsPerDay = 2 covers both tasks at once.
        var resource = new Resource { Id = 1, Name = "R1", MaxUnitsPerDay = 2 };
        var assignments = new[]
        {
            new Assignment { Id = 1, TaskId = b.Id, ResourceId = resource.Id, Units = 1 },
            new Assignment { Id = 2, TaskId = c.Id, ResourceId = resource.Id, Units = 1 },
        };

        var result = ResourceLeveler.Level(tasks, dependencies, assignments, new Dictionary<int, Resource> { [resource.Id] = resource });

        Assert.Equal(12, result.ProjectDuration);
        Assert.All(result.LeveledTasks, t => Assert.Equal(0, t.Delay));
    }
}
