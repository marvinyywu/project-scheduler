namespace Domain.Scheduling;

public sealed class ScheduleCycleException : Exception
{
    public ScheduleCycleException()
        : base("The task dependency graph contains a cycle and cannot be scheduled.")
    {
    }
}
