using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Namespaces;
using AquilaSolutions.LdesServer.Fetching.Extensions;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;
using Xunit.Abstractions;

namespace AquilaSolutions.LdesServer.Fetching.Test;

public class ProfileTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task CreateTurtleTreeProfile()
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true);
        var rdfWriter = new CompressingTurtleWriter
        {
            CompressionLevel = WriterCompressionLevel.High
        };
        
        var g = new Graph().WithStandardPrefixes();
        g.NamespaceMap.AddNamespace("ex", new Uri("https://example.org/")); // custom prefix

        // Write hypermedia controls
        var nodeUri = g.CreateUriNode(new Uri("http://localhost/"));
        var eventStreamUri = g.CreateUriNode("ex:Collection1");
        g.WithNode(nodeUri).WithEventStream(eventStreamUri).WithTriple(eventStreamUri, g.CreateUriNode(QNames.tree.view), nodeUri);
        rdfWriter.Save(g, writer);
        
        // Write each member
        var formatter = new TurtleFormatter(g);
        var members = new Tuple<string, string, int>[]
        {
            new("ex:Subject1", "Subject 1", 2),
            new("ex:Subject2", "Subject 2", 9),
        };

        foreach (var x in members)
        {
            g.Clear(); // clear all triples that have already been written
            
            // Write member separator
            var member = g.CreateUriNode(x.Item1);
            await writer.WriteLineAsync(formatter.Format(new Triple(eventStreamUri, g.CreateUriNode(QNames.tree.member), member)));

            // Write member
            var blank = g.CreateBlankNode();
            g.WithTriple(member, g.CreateUriNode(QNames.rdf.type), g.CreateUriNode("ex:Subject"))
                .WithTriple(member, g.CreateUriNode("rdfs:label"), g.CreateLiteralNode(x.Item2))
                .WithTriple(member, g.CreateUriNode("ex:value"), new LongNode(x.Item3))
                .WithTriple(member, g.CreateUriNode("ex:linkedTo"), blank)
                .WithTriple(blank, g.CreateUriNode(QNames.rdf.type), g.CreateUriNode("ex:Subject"))
                ;
            foreach (var t in g.Triples)
            {
                await writer.WriteLineAsync(formatter.Format(t));
            }
        }

        await writer.FlushAsync();

        // rewind and dump content
        stream.Seek(0, SeekOrigin.Begin);
        var content = await new StreamReader(stream).ReadToEndAsync();
        testOutputHelper.WriteLine(content);
    }
    
    [Fact]
    public async Task CreateNTriplesTreeProfile()
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true);
        var rdfWriter = new NTriplesWriter();
        
        var g = new Graph().WithStandardPrefixes();
        g.NamespaceMap.AddNamespace("ex", new Uri("https://example.org/")); // custom prefix

        // Write hypermedia controls
        var nodeUri = g.CreateUriNode(new Uri("http://localhost/"));
        var eventStreamUri = g.CreateUriNode("ex:Collection1");
        g.WithNode(nodeUri).WithEventStream(eventStreamUri).WithTriple(eventStreamUri, g.CreateUriNode(QNames.tree.view), nodeUri);
        // rdfWriter.Save(g, writer);
        
        // Write each member
        // var formatter = new NTriplesFormatter();
        var members = new Tuple<string, string, int>[]
        {
            new("ex:Subject1", "Subject 1", 2),
            new("ex:Subject2", "Subject 2", 9),
        };

        foreach (var x in members)
        {
            //g.Clear(); // clear all triples that have already been written
            
            // Write member separator
            var member = g.CreateUriNode(x.Item1);
            //await writer.WriteLineAsync(formatter.Format(new Triple(eventStreamUri, g.CreateUriNode(QNames.tree.member), member)));
            g.WithTriple(eventStreamUri, g.CreateUriNode(QNames.tree.member), member);
            
            // Write member
            var blank = g.CreateBlankNode();
            g.WithTriple(member, g.CreateUriNode(QNames.rdf.type), g.CreateUriNode("ex:Subject"))
                .WithTriple(member, g.CreateUriNode("rdfs:label"), g.CreateLiteralNode(x.Item2))
                .WithTriple(member, g.CreateUriNode("ex:value"), new LongNode(x.Item3))
                .WithTriple(member, g.CreateUriNode("ex:linkedTo"), blank)
                .WithTriple(blank, g.CreateUriNode(QNames.rdf.type), g.CreateUriNode("ex:Subject"))
                ;
            // foreach (var t in g.Triples)
            // {
            //     await writer.WriteLineAsync(formatter.Format(t));
            // }
        }

        rdfWriter.Save(g, writer);
        await writer.FlushAsync();

        // rewind and dump content
        stream.Seek(0, SeekOrigin.Begin);
        var content = await new StreamReader(stream).ReadToEndAsync();
        testOutputHelper.WriteLine(content);
    }
}