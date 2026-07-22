namespace DataToolkit.Bootstrap.Discovery;

internal readonly record struct CandidateType(
    Type Implementation,
    Type[] Services,
    ServiceRegistration Registration);