// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Models;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The All and None buttons of the node list of a skeleton set whether each node is kept, as one change to undo.
/// </summary>
public sealed class SkeletonNodePreserveAllCommandTests
{
    [Fact]
    public async Task AllAndNoneSetThePreserveFlagOfEachNode()
    {
        var skeleton = new SkeletonAsset();
        skeleton.Nodes.Add(new NodeInformation("Root", 0, true));
        skeleton.Nodes.Add(new NodeInformation("Spine", 1, false));
        skeleton.Nodes.Add(new NodeInformation("Head", 2, true));
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Skeleton", skeleton));
        fixture.Session.AssetViewProperties.RegisterNodePresenterCommand(new SkeletonNodePreserveAllCommand());
        var asset = fixture.Asset("Skeleton");
        var nodes = ((SkeletonAsset)asset.Asset).Nodes;
        var undoRedo = fixture.Session.UndoRedoService;
        var steps = 0;
        undoRedo.Done += (_, _) => steps++;

        var root = await fixture.PropertiesOf(asset);
        var list = root.GetChild(nameof(SkeletonAsset.Nodes))!;
        var command = list.GetCommand(SkeletonNodePreserveAllCommand.CommandName)!;

        command.Execute(true);
        Assert.All(nodes, x => Assert.True(x.Preserve));
        Assert.Equal(1, steps);

        command.Execute(false);
        Assert.All(nodes, x => Assert.False(x.Preserve));
        Assert.Equal(2, steps);
        // The names stay: only the flags change
        Assert.Equal(["Root", "Spine", "Head"], nodes.Select(x => x.Name));

        undoRedo.Undo();
        Assert.Equal([true, true, true], nodes.Select(x => x.Preserve));
    }
}
