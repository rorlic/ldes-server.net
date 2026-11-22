namespace LdesServer.Serving;

public class ServingConfiguration
{
    public int MaxAge { get; set; } = 60;
    public int MaxAgeImmutable { get; set; } = 604800;

    // Overload protection (responds with 429 Too Many Requests)
    public int MaxConcurrentRequests { get; set; } = 64;   // keep below the Npgsql pool size (default 100)
    public int QueueLimit { get; set; } = 0;               // 0 = reject immediately instead of queueing
    public int RetryAfterSeconds { get; set; } = 5;
}