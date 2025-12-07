// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Quantum.Tests;

public class TestQuantumConsistencyException
{
    public class SimpleClass
    {
        public int Value { get; set; }
    }

    [Fact]
    public void TestExceptionWithSimpleStrings()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        var exception = new QuantumConsistencyException("Expected state", "Observed state", node);

        Assert.Equal("Expected state", exception.Expected);
        Assert.Equal("Observed state", exception.Observed);
        Assert.Equal(node, exception.Node);
        Assert.Contains("Expected state", exception.Message);
        Assert.Contains("Observed state", exception.Message);
    }

    [Fact]
    public void TestExceptionWithFormattedStrings()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        var exception = new QuantumConsistencyException(
            "Expected: {0}",
            "value 1",
            "Observed: {0}",
            "value 2",
            node);

        Assert.Contains("value 1", exception.Expected);
        Assert.Contains("value 2", exception.Observed);
        Assert.Equal(node, exception.Node);
        Assert.Contains("Expected", exception.Message);
        Assert.Contains("Observed", exception.Message);
    }

    [Fact]
    public void TestExceptionWithNullStrings()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        var exception = new QuantumConsistencyException(null, null, node);

        Assert.Contains("NullMessage", exception.Expected);
        Assert.Contains("NullMessage", exception.Observed);
        Assert.Equal(node, exception.Node);
    }

    [Fact]
    public void TestExceptionWithNullFormattedStrings()
    {
        var obj = new SimpleClass { Value = 42 };
        var nodeContainer = new NodeContainer();
        var node = nodeContainer.GetOrCreateNode(obj);

        var exception = new QuantumConsistencyException(null, null, null, null, node);

        Assert.Contains("NullMessage", exception.Expected);
        Assert.Contains("NullMessage", exception.Observed);
        Assert.Equal(node, exception.Node);
    }
}
