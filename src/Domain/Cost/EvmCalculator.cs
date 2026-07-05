using Domain.Entities;

namespace Domain.Cost;

public readonly record struct EvmReport(
	int AsOfDay,
	decimal Bac,
	decimal Pv,
	decimal Ev,
	decimal Ac,
	decimal Sv,
	decimal Cv,
	double Spi,
	double Cpi,
	decimal Eac,
	decimal Etc,
	decimal Vac);

public static class EvmCalculator
{
	public static EvmReport Compute(IReadOnlyList<ScheduleTask> tasks, int asOfDay)
	{
		var bac = tasks.Sum(t => t.Budget);
		var pv = tasks.Sum(t => PlannedValue(t, asOfDay));
		var ev = tasks.Sum(t => t.Budget * (decimal)(t.PercentComplete / 100.0));
		var ac = tasks.Sum(t => t.ActualCost);

		var sv = ev - pv;
		var cv = ev - ac;
		var spi = pv == 0 ? 0 : (double)(ev / pv);
		var cpi = ac == 0 ? 0 : (double)(ev / ac);
		var eac = cpi == 0 ? bac : bac / (decimal)cpi;
		var etc = eac - ac;
		var vac = bac - eac;

		return new EvmReport(asOfDay, bac, pv, ev, ac, sv, cv, spi, cpi, eac, etc, vac);
	}

	private static decimal PlannedValue(ScheduleTask task, int asOfDay)
	{
		if (task.Duration == 0 || asOfDay <= task.EarlyStart)
		{
			return 0;
		}

		if (asOfDay >= task.EarlyFinish)
		{
			return task.Budget;
		}

		var fraction = (decimal)(asOfDay - task.EarlyStart) / task.Duration;
		return task.Budget * fraction;
	}
}