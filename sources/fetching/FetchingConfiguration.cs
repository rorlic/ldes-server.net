namespace AquilaSolutions.LdesServer.Fetching;

public class FetchingConfiguration
{
    public int MaxAge { get; set; } = 60;
    public int MaxAgeImmutable { get; set; } = 604800;
}