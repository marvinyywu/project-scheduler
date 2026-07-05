using Domain.Cost;
using Domain.Entities;
using Xunit;

namespace Domain.Tests;

public class EvmCalculatorTests
{
    [Fact]
    public void ComputesRollUp_ForWorkedExample_AtStatusDayFive()
    {
        var a = new ScheduleTask { Id = 1, Name = "A", Duration = 3, EarlyStart = 0, EarlyFinish = 3, Budget = 300m, PercentComplete = 100, ActualCost = 320m };
        var b = new ScheduleTask { Id = 2, Name = "B", Duration = 4, EarlyStart = 3, EarlyFinish = 7, Budget = 400m, PercentComplete = 60, ActualCost = 250m };
        var c = new ScheduleTask { Id = 3, Name = "C", Duration = 2, EarlyStart = 3, EarlyFinish = 5, Budget = 200m, PercentComplete = 100, ActualCost = 180m };
        var d = new ScheduleTask { Id = 4, Name = "D", Duration = 5, EarlyStart = 7, EarlyFinish = 12, Budget = 500m, PercentComplete = 0, ActualCost = 0m };
        var tasks = new[] { a, b, c, d };

        var report = EvmCalculator.Compute(tasks, asOfDay: 5);

        Assert.Equal(1400m, report.Bac);
        Assert.Equal(700m, report.Pv);
        Assert.Equal(740m, report.Ev);
        Assert.Equal(750m, report.Ac);
        Assert.Equal(40m, report.Sv);
        Assert.Equal(-10m, report.Cv);
        Assert.Equal(1.0571, report.Spi, 4);
        Assert.Equal(0.9867, report.Cpi, 4);
        Assert.Equal(1418.92, (double)report.Eac, 2);
        Assert.Equal(668.92, (double)report.Etc, 2);
        Assert.Equal(-18.92, (double)report.Vac, 2);
    }

    [Fact]
    public void Spi_IsZero_NotDivideByZero_WhenNothingScheduledYet()
    {
        var task = new ScheduleTask { Id = 1, Name = "A", Duration = 5, EarlyStart = 10, EarlyFinish = 15, Budget = 100m, PercentComplete = 0, ActualCost = 0m };

        var report = EvmCalculator.Compute([task], asOfDay: 0);

        Assert.Equal(0m, report.Pv);
        Assert.Equal(0, report.Spi);
    }

    [Fact]
    public void Cpi_IsZero_NotDivideByZero_WhenNothingSpentYet()
    {
        var task = new ScheduleTask { Id = 1, Name = "A", Duration = 5, EarlyStart = 0, EarlyFinish = 5, Budget = 100m, PercentComplete = 50, ActualCost = 0m };

        var report = EvmCalculator.Compute([task], asOfDay: 3);

        Assert.Equal(0m, report.Ac);
        Assert.Equal(0, report.Cpi);
        Assert.Equal(report.Bac, report.Eac);
    }
}
