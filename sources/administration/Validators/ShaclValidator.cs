using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Shacl;

namespace AquilaSolutions.LdesServer.Administration.Validators;

public class ShaclValidator : AbstractValidator<IGraph>, IDisposable
{
    private readonly ShapesGraph _shapesGraph;

    public IGraph Shacl => _shapesGraph;

    private ShapesGraph CreateShapes(string schema)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name;
        var resourceName = $"{assemblyName}.Resources.{schema}";
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        var parser = new TurtleParser();
        var shapes = new Graph();
        parser.Load(shapes, reader);
        return new ShapesGraph(shapes);
    }

    protected ShaclValidator(string schema)
    {
        _shapesGraph = CreateShapes(schema);
        
        RuleFor(x => x).Must((x, _, c) =>
        {
            var report = _shapesGraph.Validate(x);
            var isValid = report.Conforms;
            report.Results.ToList().ForEach(e => c.AddFailure(
                new ValidationFailure(e.ResultPath.ToString(), e.Message.Value)
                    { Severity = AsSeverity(e.Severity.ToString()) }));
            return isValid;
        }).WithMessage("SHACL validation failed.");
    }

    private static Severity AsSeverity(string severity)
    {
        return severity switch
        {
            "http://www.w3.org/ns/shacl#Violation" => Severity.Error,
            "http://www.w3.org/ns/shacl#Warning" => Severity.Warning,
            "http://www.w3.org/ns/shacl#Info" => Severity.Info,
            _ => throw new ArgumentOutOfRangeException($"Unknown shacl severity: {severity}")
        };
    }

    public void Dispose()
    {
        _shapesGraph.Dispose();
        GC.SuppressFinalize(this);
    }
}