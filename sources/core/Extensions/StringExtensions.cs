using VDS.RDF;

namespace AquilaSolutions.LdesServer.Core.Extensions;

public static class StringExtensions
{
    public static ILiteralNode AsLiteralNode(this string s, IGraph g)
    {
        if (s.Contains('@'))
        {
            var parts = s.Split('@');
            return g.CreateLiteralNode(parts[0].Trim('"'), parts[1]);
        }

        if (s.Contains("^^"))
        {
            var parts = s.Split("^^");
            return g.CreateLiteralNode(parts[0].Trim('"'), g.CreateUriNode(parts[1]).Uri);
        }

        return g.CreateLiteralNode(s);
    }
}