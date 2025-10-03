using System.Globalization;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;

public abstract class BySubjectAndPredicateStrategyBase : IIdentifyVersionStrategy
{
    private const string DateTimeType = "http://www.w3.org/2001/XMLSchema#dateTime";
    private const string StringType = "http://www.w3.org/2001/XMLSchema#string";

    public ILiteralNode FindOrCreateEntityVersion(IEnumerable<Quad> quads, IUriNode subject, DateTimeOffset createdAt)
    {
        var literalNode = SearchLiteralNode(quads, subject);
        var dataType = literalNode.DataType.AbsoluteUri;

        return dataType switch
        {
            DateTimeType => literalNode,
            // Note: normally we expect a timestamp, but if the string literal can be parsed as a date
            //       then we allow that too, but we fix the data type to be a xml:dateTime.
            StringType when DateTimeOffset.TryParse(literalNode.Value, null, DateTimeStyles.RoundtripKind, out _) =>
                new LiteralNode(literalNode.Value, new Uri(DateTimeType)),
            _ => throw new ArgumentException(
                $"The matched version identifier does not have the correct data type: '{dataType}' (expected a {DateTimeType} or a {StringType}).'")
        };
    }

    protected abstract ILiteralNode SearchLiteralNode(IEnumerable<Quad> entity, IUriNode subject);
}