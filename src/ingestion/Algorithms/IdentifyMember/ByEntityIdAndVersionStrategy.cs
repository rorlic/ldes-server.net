using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyMember;

/// <summary>
/// This strategy creates a member identifier (URI) based on an entity identifier, a configured separator and
/// the entity version value.
/// </summary>
/// <param name="separator">An optional property (defaults to "/") that acts as a separator
/// between the entity identifier and its version value.</param>
/// <returns>A new URI concatenating the entity URI value, the separator and the entity version value.</returns>
public class ByEntityIdAndVersionStrategy(string? separator) : IIdentifyMemberStrategy
{
    private string Separator { get; } = separator ?? "/";

    public IUriNode FindOrCreateMemberIdentifier(IEnumerable<Quad> quads, IUriNode entityId, ILiteralNode version)
    {
        return new UriNode(new Uri($"{entityId.Uri.AbsoluteUri}{Separator}{version.Value}"));
    }
}