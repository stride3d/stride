// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Models;
using Stride.Assets.Models.Humanoid;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The humanoid bones of a skeleton: Auto-map fills them from the node names as one change to undo, each entry picks its node
/// in the nodes of the skeleton, and a row below the list names the required bones still unmapped.
/// </summary>
public sealed class HumanoidAutoMapCommandTests
{
    [Fact]
    public async Task AutoMapFillsTheBonesAndUndoesInOneStep()
    {
        var skeleton = new SkeletonAsset();
        foreach (var (name, depth) in new[]
        {
            ("Hips", 0), ("Spine", 1), ("Neck", 2), ("Head", 3),
            ("LeftArm", 2), ("LeftForeArm", 3), ("LeftHand", 4), ("RightArm", 2), ("RightForeArm", 3), ("RightHand", 4),
            ("LeftUpLeg", 1), ("LeftLeg", 2), ("LeftFoot", 3), ("RightUpLeg", 1), ("RightLeg", 2), ("RightFoot", 3), ("Prop", 1),
        })
        {
            skeleton.Nodes.Add(new NodeInformation(name, depth, false));
        }
        // Set by hand: auto-map keeps it
        skeleton.HumanoidBones.Add(new HumanoidBoneMapping { Bone = HumanoidBone.Head, NodeName = "Neck" });

        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Skeleton", skeleton));
        fixture.Session.AssetViewProperties.RegisterNodePresenterCommand(new HumanoidAutoMapCommand());
        fixture.Session.AssetViewProperties.RegisterNodePresenterUpdater(new SkeletonAssetNodeUpdater());
        var asset = fixture.Asset("Skeleton");
        var assetSkeleton = (SkeletonAsset)asset.Asset;
        var undoRedo = fixture.Session.UndoRedoService;
        var steps = 0;
        undoRedo.Done += (_, _) => steps++;

        var root = await fixture.PropertiesOf(asset);
        var list = root.GetChild(nameof(SkeletonAsset.HumanoidBones))!;
        // Each entry picks its node in the nodes of the skeleton
        Assert.True(Assert.Single(list.Children).AssociatedData.ContainsKey(ModelNodeLinkData.AvailableNodes));
        var unmapped = root.GetChild(SkeletonAssetNodeUpdater.UnmappedRequiredBones)!;
        Assert.True(unmapped.IsReadOnly);
        Assert.StartsWith("Hips, Spine, LeftUpperArm", (string)unmapped.NodeValue!);

        list.GetCommand(HumanoidAutoMapCommand.CommandName)!.Execute(null);

        Assert.Equal(1, steps);
        var mapping = assetSkeleton.HumanoidBones.ToDictionary(x => x.Bone, x => x.NodeName);
        Assert.Equal("Neck", mapping[HumanoidBone.Head]);
        Assert.Equal("LeftForeArm", mapping[HumanoidBone.LeftLowerArm]);
        Assert.Equal("RightUpLeg", mapping[HumanoidBone.RightUpperLeg]);
        // The Neck node is the head already: it is not mapped to the neck bone
        Assert.DoesNotContain(HumanoidBone.Neck, mapping.Keys);
        Assert.Equal("None", (string)root.GetChild(SkeletonAssetNodeUpdater.UnmappedRequiredBones)!.NodeValue!);
        // The mapped nodes are kept in the compiled skeleton
        Assert.All(assetSkeleton.Nodes.Where(x => x.Name is not ("Prop" or "Neck" or "Head")), x => Assert.True(x.Preserve, x.Name));
        Assert.False(assetSkeleton.Nodes.Single(x => x.Name == "Prop").Preserve);

        undoRedo.Undo();
        Assert.Equal(HumanoidBone.Head, Assert.Single(assetSkeleton.HumanoidBones).Bone);
        Assert.All(assetSkeleton.Nodes, x => Assert.False(x.Preserve, x.Name));
    }
}
