using System.IO.Compression;
using LdesServer.Core.Interfaces;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing.Formatting;

namespace LdesServer.Core.Models;

public class Member : IMember
{
    public required byte[] EntityModel { get; set; }
    public required string MemberId { get; set; }
    public required string EntityId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    private const int OneKiB = 1024;

    public static Member From(IEnumerable<Quad> quads, IUriNode memberId, IUriNode entityId, DateTimeOffset createdAt)
    {
        using var stream = new MemoryStream(4 * OneKiB);
        using var zipper = new GZipStream(stream, CompressionMode.Compress);
        using var writer = new StreamWriter(zipper);

        // NOTE: we drop the named graph as we either return the triples as-is wrapped in the memberId graph or transform 
        //       the member into a version by substituting each entityId subject to memberId (see ToVersionTriples below).
        var formatter = new NTriples11Formatter(); 
        
        quads.Select(x => formatter.Format(x.AsTriple())).ToList().ForEach(writer.WriteLine);
        writer.Flush();

        return new Member
        {
            EntityModel = stream.ToArray(),
            MemberId = memberId.ToString(),
            EntityId = entityId.ToString(),
            CreatedAt = createdAt
        };
    }

    public IEnumerable<Triple> ToTriples()
    {
        using var g = new Graph();
        var parser = new NTriplesParser(NTriplesSyntax.Rdf11);

        using var stream = new MemoryStream(EntityModel);
        using var unzipper = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(unzipper);
        
        parser.Load(g, reader);
        return g.Triples.ToArray();
    }
}