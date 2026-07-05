using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class BaselineConfiguration : IEntityTypeConfiguration<Baseline>
{
    public void Configure(EntityTypeBuilder<Baseline> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.SnapshotJson).IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}