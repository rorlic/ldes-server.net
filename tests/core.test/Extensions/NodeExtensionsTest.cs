using LdesServer.Core.Extensions;
using FluentAssertions;
using VDS.RDF;

namespace LdesServer.Core.Test.Extensions;

public class NodeExtensionsTest
{
    [Fact]
    public void CanConvertToString()
    {
        const string example = "https://example.org";
        new UriNode(new Uri(example)).AsValueString().Should().Be($"{example}/");
        new LiteralNode(example).AsValueString().Should().Be(example);
        new LiteralNode(example, new Uri(example)).AsValueString().Should().Be(example);
    }

    [Fact]
    public void CanCastToUriNode()
    {
        const string example = "https://example.org";
        var node = new UriNode(new Uri(example));

        node.AsUriNode().Should().Be(node);
    }

    [Fact]
    public void ThrowsInvalidCastIfCannotCastToUriNode()
    {
        const string example = "https://example.org";
        var node = new LiteralNode(example);

        var action = void () => node.AsUriNode();
        action.Should().Throw<InvalidCastException>();
    }
}