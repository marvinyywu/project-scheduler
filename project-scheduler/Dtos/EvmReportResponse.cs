using Domain.Cost;

namespace project_scheduler.Dtos;

public sealed record EvmReportResponse(
    int AsOfDay, decimal Bac, decimal Pv, decimal Ev, decimal Ac,
    decimal Sv, decimal Cv, double Spi, double Cpi, decimal Eac, decimal Etc, decimal Vac)
{
    public static EvmReportResponse FromDomain(EvmReport report) => new(
        report.AsOfDay, report.Bac, report.Pv, report.Ev, report.Ac,
        report.Sv, report.Cv, report.Spi, report.Cpi, report.Eac, report.Etc, report.Vac);
}