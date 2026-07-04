using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DependencyConfiguration : IEntityTypeConfiguration<Dependency>
{
    public void Configure(EntityTypeBuilder<Dependency> builder)
    {
        builder.HasKey(d => d.Id);

        // Dependency has two FKs into Tasks (Predecessor/Successor); both must be
        // Restrict, otherwise SQL Server rejects the model with multiple cascade paths.
        builder.HasOne<ScheduleTask>()
            .WithMany()
            .HasForeignKey(d => d.PredecessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ScheduleTask>()
            .WithMany()
            .HasForeignKey(d => d.SuccessorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
