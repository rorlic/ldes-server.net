namespace AquilaSolutions.LdesServer.Core.Models.Configuration;

public class LdesServerConfiguration
{
    [Obsolete]
    public bool Compatible { get; set; } // TODO: remove this after adding VSDS migration API
    public string BaseUri { get; set; } = "http://localhost:8080/feed/";
    public bool CreateEventSource { get; set; } = true;
    public string? DefinitionsDirectory { get; set; }
    
    public Uri GetBaseUri() => new(BaseUri);
}