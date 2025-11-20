namespace LdesServer.Administration.Validators;

/// <summary>
/// Collection validator based on SHACL shapes
/// </summary>
public class CollectionValidator() : ShaclValidator("Collection.schema.ttl");