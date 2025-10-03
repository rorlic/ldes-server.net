namespace AquilaSolutions.LdesServer.Administration.Validators;

/// <summary>
/// View validator based on SHACL shapes
/// </summary>
public class ViewValidator() : ShaclValidator("View.schema.ttl");