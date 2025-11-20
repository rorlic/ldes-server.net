namespace LdesServer.Serving;

public class ServingConfiguration
{
    public int MaxAge { get; set; } = 60;
    public int MaxAgeImmutable { get; set; } = 604800;
}