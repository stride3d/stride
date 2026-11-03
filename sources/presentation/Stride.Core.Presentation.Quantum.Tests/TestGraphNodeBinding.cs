// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Tests;

public class TestGraphNodeBinding
{
    [Fact]
    public void TestNullConverterIsRejected()
    {
        var node = CreateNameNode();

        Assert.Throws<ArgumentNullException>(() => new MemberGraphNodeBinding<string, string>(node, "Name", _ => { }, _ => { }, null!, null));
    }

    [Fact]
    public void TestConverterWritesTheValue()
    {
        var node = CreateNameNode();

        using var binding = new MemberGraphNodeBinding<string, string>(node, "Name", _ => { }, _ => { }, x => x?.ToUpperInvariant(), null);
        binding.SetNodeValue("Changed");

        Assert.Equal("CHANGED", binding.GetNodeValue());
    }

    private static IMemberNode CreateNameNode()
    {
        var context = new TestContainerContext();
        var rootNode = context.NodeContainer.GetOrCreateNode(new Types.SimpleObject { Name = "Initial" });
        return rootNode[nameof(Types.SimpleObject.Name)];
    }
}
