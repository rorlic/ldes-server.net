namespace LdesServer.Core.Models.Configuration;

public class LdesServerConfiguration
{
    [Obsolete]
    public string BaseUri { get; set; } = "http://localhost:8080/feed/";
    public bool CreateEventSource { get; set; } = true;
    public string? DefinitionsDirectory { get; set; }
    
    public Uri GetBaseUri() => new(BaseUri);
}