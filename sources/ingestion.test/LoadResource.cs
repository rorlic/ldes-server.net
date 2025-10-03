using System.Reflection;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Test;

public static class LoadResource
{
    public static Stream GetEmbeddedStream(string resourceName)
    {
        return Core.Test.LoadResource.GetEmbeddedStream(resourceName, Assembly.GetCallingAssembly());
    }
    public static IEnumerable<Quad> FromTurtle(string resourceName)
    {
        return Core.Test.LoadResource.FromTurtle(resourceName, Assembly.GetCallingAssembly());
    }
    
    public static IEnumerable<Quad> FromTrig(string resourceName)
    {
        return Core.Test.LoadResource.FromTrig(resourceName, Assembly.GetCallingAssembly());

    }
}
