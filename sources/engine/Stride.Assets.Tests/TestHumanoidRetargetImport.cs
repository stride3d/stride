// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Animations;
using Stride.Assets.Models;
using Stride.Assets.Models.Humanoid;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Importer.ThreeD;
using Stride.Rendering;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// The retargeting of a real animation file: the walk of the mannequin pack, from the mannequin onto itself.
/// </summary>
/// <remarks>
/// The skeleton asset is a copy of the pack's, since the test lanes check out without the samples.
/// </remarks>
public class TestHumanoidRetargetImport
{
    private static readonly string ModelPath = Path.Combine(AppContext.BaseDirectory, "app_data", "Stride.Assets.Tests", "bones", "mannequinModel.fbx");
    private static readonly string AnimationPath = Path.Combine(AppContext.BaseDirectory, "app_data", "Stride.Assets.Tests", "retarget", "Walk.fbx");
    private static readonly string SkeletonPath = Path.Combine(AppContext.BaseDirectory, "app_data", "Stride.Assets.Tests", "retarget", "mannequinModel Skeleton.sdskel");

    [Fact]
    public void SameSkeletonGivesThePlainImport()
    {
        var skeleton = new MeshConverter(null).ConvertSkeleton(ModelPath, ModelPath);
        var fileSkeleton = new MeshConverter(null).ConvertSkeleton(AnimationPath, AnimationPath);
        var animation = new MeshConverter(null).ConvertAnimation(AnimationPath, AnimationPath, 0);
        var mapping = MapMannequin(skeleton);

        var command = new ImportThreeDCommand { Retarget = new() { SourceBones = mapping, TargetBones = mapping } };
        var clip = new AnimationClip { Duration = animation.Duration };
        command.RetargetAnimation(skeleton, fileSkeleton, skeleton, animation.AnimationClips, animation.Duration, clip);

        // A rotation curve per mapped node and the hips position, and the curves of the file on the unmapped nodes under the hips
        var mappedNodes = mapping.Select(x => Array.FindIndex(skeleton.Nodes, n => n.Name == x.NodeName)).ToArray();
        var hips = mappedNodes[mapping.FindIndex(x => x.Bone == HumanoidBone.Hips)];
        var bodyNodes = Enumerable.Range(0, skeleton.Nodes.Length).Where(x => IsUnder(skeleton, x, hips)).ToArray();
        var copiedNodes = bodyNodes.Except(mappedNodes).Where(x => animation.AnimationClips.ContainsKey(skeleton.Nodes[x].Name)).ToArray();
        Assert.NotEmpty(copiedNodes);
        var copiedChannels = copiedNodes.Sum(x => animation.AnimationClips[skeleton.Nodes[x].Name].Channels.Count);
        Assert.Equal(mappedNodes.Length + 1 + copiedChannels, clip.Channels.Count);
        var hipsPositions = Curve<Vector3>(clip, $"[ModelComponent.Key].Skeleton.NodeTransformations[{hips}].Transform.Position");
        Assert.True(hipsPositions.KeyFrames.Count > 10);

        var plainLocals = new TransformTRS[skeleton.Nodes.Length];
        var retargetedLocals = new TransformTRS[skeleton.Nodes.Length];
        var plainWorld = new Matrix[skeleton.Nodes.Length];
        var retargetedWorld = new Matrix[skeleton.Nodes.Length];
        for (var key = 0; key < hipsPositions.KeyFrames.Count; ++key)
        {
            var time = hipsPositions.KeyFrames[key].Time;

            // The plain import plays the curves of the file on the nodes of the same name
            for (var i = 0; i < skeleton.Nodes.Length; ++i)
                plainLocals[i] = retargetedLocals[i] = skeleton.Nodes[i].Transform;
            foreach (var (nodeName, nodeClip) in animation.AnimationClips)
            {
                var node = Array.FindIndex(skeleton.Nodes, x => x.Name == nodeName);
                if (node < 0)
                    continue;
                foreach (var (channelName, channel) in nodeClip.Channels)
                {
                    var curve = nodeClip.Curves[channel.CurveIndex];
                    if (channelName == "Transform.Position")
                        plainLocals[node].Position = ImportModelCommand.Sample((AnimationCurve<Vector3>)curve, time, Vector3.Lerp);
                    else if (channelName == "Transform.Rotation")
                        plainLocals[node].Rotation = ImportModelCommand.Sample((AnimationCurve<Quaternion>)curve, time, (a, b, amount) => Quaternion.Slerp(a, b, amount));
                    else if (channelName == "Transform.Scale")
                        plainLocals[node].Scale = ImportModelCommand.Sample((AnimationCurve<Vector3>)curve, time, Vector3.Lerp);
                }
            }

            // The retargeted clip: every curve is keyed at the same times
            foreach (var (channelName, channel) in clip.Channels)
            {
                var node = int.Parse(channelName.Split('[', ']')[3]);
                var curve = clip.Curves[channel.CurveIndex];
                if (channelName.EndsWith(".Rotation", StringComparison.Ordinal))
                {
                    var keyFrame = ((AnimationCurve<Quaternion>)curve).KeyFrames[key];
                    Assert.Equal(time, keyFrame.Time);
                    retargetedLocals[node].Rotation = keyFrame.Value;
                }
                else
                {
                    var keyFrame = ((AnimationCurve<Vector3>)curve).KeyFrames[key];
                    Assert.Equal(time, keyFrame.Time);
                    if (channelName.EndsWith(".Position", StringComparison.Ordinal))
                        retargetedLocals[node].Position = keyFrame.Value;
                    else
                        retargetedLocals[node].Scale = keyFrame.Value;
                }
            }

            HumanoidRetargeter.ComputeWorldMatrices(skeleton.Nodes, plainLocals, plainWorld);
            HumanoidRetargeter.ComputeWorldMatrices(skeleton.Nodes, retargetedLocals, retargetedWorld);
            foreach (var node in bodyNodes)
            {
                plainWorld[node].Decompose(out _, out Quaternion expected, out var expectedPosition);
                retargetedWorld[node].Decompose(out _, out Quaternion actual, out var actualPosition);
                TestHumanoidRetarget.AssertClose(expected, actual, 1e-3f, $"{skeleton.Nodes[node].Name} at {time}");
                Assert.True(Vector3.Distance(expectedPosition, actualPosition) < 1e-3f, $"{skeleton.Nodes[node].Name} at {time}: expected {expectedPosition}, got {actualPosition}");
            }
        }
    }

    private static bool IsUnder(Skeleton skeleton, int node, int ancestor)
    {
        for (; node >= 0; node = skeleton.Nodes[node].ParentIndex)
        {
            if (node == ancestor)
                return true;
        }
        return false;
    }

    [Fact]
    public void OptionalBoneTheFileLacksIsLeftUnmapped()
    {
        var skeleton = new MeshConverter(null).ConvertSkeleton(ModelPath, ModelPath);
        var fileSkeleton = new MeshConverter(null).ConvertSkeleton(AnimationPath, AnimationPath);
        var animation = new MeshConverter(null).ConvertAnimation(AnimationPath, AnimationPath, 0);
        var mapping = MapMannequin(skeleton);

        // The source skeleton has a left eye that the file does not animate
        var head = Array.FindIndex(skeleton.Nodes, x => x.Name == mapping.Single(m => m.Bone == HumanoidBone.Head).NodeName);
        var eye = new ModelNodeDefinition { Name = "LeftEyeOfSource", ParentIndex = head, Transform = new TransformTRS { Rotation = Quaternion.Identity, Scale = Vector3.One } };
        var source = new Skeleton { Nodes = [.. skeleton.Nodes, eye] };
        var sourceMapping = mapping.Append(new HumanoidBoneMapping { Bone = HumanoidBone.LeftEye, NodeName = eye.Name }).ToList();

        var expected = new AnimationClip { Duration = animation.Duration };
        new ImportThreeDCommand { Retarget = new() { SourceBones = mapping, TargetBones = mapping } }
            .RetargetAnimation(skeleton, fileSkeleton, skeleton, animation.AnimationClips, animation.Duration, expected);
        var actual = new AnimationClip { Duration = animation.Duration };
        new ImportThreeDCommand { Retarget = new() { SourceBones = sourceMapping, TargetBones = mapping } }
            .RetargetAnimation(source, fileSkeleton, skeleton, animation.AnimationClips, animation.Duration, actual);

        Assert.Equal(expected.Channels.Keys.Order(), actual.Channels.Keys.Order());
    }

    [Fact]
    public void ImportDependsOnItsSkeletons()
    {
        var command = new ImportThreeDCommand { Mode = ImportModelCommand.ExportMode.Animation, SourcePath = AnimationPath, SkeletonUrl = "Models/Target Skeleton" };
        var file = new ObjectUrl(UrlType.File, command.SourcePath);
        var target = new ObjectUrl(UrlType.Content, "Models/Target Skeleton");
        Assert.Equal([file, target], command.GetInputFiles());

        command.Retarget = new() { SourceSkeletonUrl = "Mocap/Source Skeleton" };
        Assert.Equal([file, target, new ObjectUrl(UrlType.Content, "Mocap/Source Skeleton")], command.GetInputFiles());

        command.Retarget = null;
        command.SkeletonUrl = null;
        Assert.Equal([file], command.GetInputFiles());

        command.Mode = ImportModelCommand.ExportMode.Model;
        command.SkeletonUrl = "Models/Target Skeleton";
        Assert.Equal([file, target], command.GetInputFiles());
    }

    [Fact]
    public void MannequinSkeletonMapsItsHumanoidBones()
    {
        var asset = LoadMannequinSkeleton();

        Assert.Empty(HumanoidBones.GetMissingRequired(asset.HumanoidBones));
        Assert.Equal(asset.HumanoidBones.Count, asset.HumanoidBones.Select(x => x.Bone).Distinct().Count());
        Assert.All(asset.HumanoidBones, x => Assert.True(asset.Nodes.Single(n => n.Name == x.NodeName).Preserve, x.NodeName));
        // The CAT names of its neck and head are custom: they are picked by hand
        Assert.Equal("CATRigSpin4435f", asset.HumanoidBones.Single(x => x.Bone == HumanoidBone.Neck).NodeName);
        Assert.Equal("CATRigRibcage0rtee", asset.HumanoidBones.Single(x => x.Bone == HumanoidBone.Head).NodeName);
    }

    private static SkeletonAsset LoadMannequinSkeleton() => AssetFileSerializer.Load<SkeletonAsset>(SkeletonPath).Asset;

    /// <summary>
    /// The humanoid bones of the mannequin, as in its skeleton asset.
    /// </summary>
    private static List<HumanoidBoneMapping> MapMannequin(Skeleton skeleton)
    {
        var mapping = LoadMannequinSkeleton().HumanoidBones;
        Assert.All(mapping, x => Assert.Contains(skeleton.Nodes, n => n.Name == x.NodeName));
        return mapping;
    }

    private static AnimationCurve<T> Curve<T>(AnimationClip clip, string channel)
        => (AnimationCurve<T>)clip.Curves[clip.Channels[channel].CurveIndex];
}
