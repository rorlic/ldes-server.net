// ReSharper disable InconsistentNaming

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
namespace AquilaSolutions.LdesServer.Core.Namespaces;

public static class QNames
{
    public static class rdf
    {
        public const string type = "rdf:type";
    }

    public static class ldes
    {
        public const string EventStream = "ldes:EventStream";
        public const string versionOfPath = "ldes:versionOfPath";
        public const string timestampPath = "ldes:timestampPath";
        public const string EventSource = "ldes:EventSource";
        public const string immutable = "ldes:immutable";

        public const string createVersions = "ldes:createVersions"; // NOTE: not an official predicate
        public const string versionDelimiter = "ldes:versionDelimiter"; // NOTE: not an official predicate
    }

    public static class tree
    {
        public const string view = "tree:view";
        public const string relation = "tree:relation";
        public const string Relation = "tree:Relation";
        public const string node = "tree:node";
        public const string Node = "tree:Node";
        public const string member = "tree:member";
        public const string viewDescription = "tree:viewDescription";
        public const string path = "tree:path";
        public const string value = "tree:value";
        public const string GreaterThanOrEqualToRelation = "tree:GreaterThanOrEqualToRelation";
        public const string LessThanRelation = "tree:LessThanRelation";

        public const string pageSize = "tree:pageSize"; // NOTE: not an official predicate 
        public const string fragmentationStrategy = "tree:fragmentationStrategy"; // NOTE: not an official predicate 
    }

    public static class dct
    {
        public const string isVersionOf = "dct:isVersionOf";
        // public const string created = "dct:created";
    }

    public static class prov
    {
        public const string generatedAtTime = "prov:generatedAtTime";
    }

    public static class lsdn // NOTE: not an official prefix
    {
        public const string TimeFragmentation = "lsdn:TimeFragmentation";
        public const string bucket = "lsdn:bucket";
    }
}