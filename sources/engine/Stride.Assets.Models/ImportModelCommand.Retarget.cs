// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Animations;
using Stride.Assets.Models.Humanoid;
using Stride.Core;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;

namespace Stride.Assets.Models
{
    public partial class ImportModelCommand
    {
        /// <summary>
        /// Gets or sets how to retarget the animation from the skeleton it was made for, or <c>null</c> to import it on node names.
        /// </summary>
        /// <remarks>
        /// Not serialized with the other parameters: <see cref="ComputeParameterHash"/> adds it only when it is set.
        /// </remarks>
        [DataMemberIgnore]
        internal HumanoidRetargetParameters Retarget { get; set; }

        /// <summary>
        /// The inputs of a humanoid retargeting: the skeleton the animation was made for, and the humanoid bones of both skeletons.
        /// </summary>
        [DataContract]
        internal sealed class HumanoidRetargetParameters
        {
            /// <summary>
            /// The URL of the compiled skeleton the animation was made for.
            /// </summary>
            public string SourceSkeletonUrl { get; set; }

            /// <summary>
            /// The scale import of the source skeleton, applied to the positions of the animation file.
            /// </summary>
            public float SourceScaleImport { get; set; } = 1.0f;

            /// <summary>
            /// The pivot position of the source skeleton, applied to the positions of the animation file.
            /// </summary>
            public Vector3 SourcePivotPosition { get; set; }

            /// <summary>
            /// The humanoid bones of the source skeleton.
            /// </summary>
            public List<HumanoidBoneMapping> SourceBones { get; set; } = [];

            /// <summary>
            /// The humanoid bones of the target skeleton, the one of <see cref="SkeletonUrl"/>.
            /// </summary>
            public List<HumanoidBoneMapping> TargetBones { get; set; } = [];
        }

        /// <summary>
        /// Adds to <paramref name="animationClip"/> the curves of the target skeleton nodes, retargeted from the node curves of the file.
        /// </summary>
        /// <param name="fileSkeleton">The nodes of the animation file, adjusted with the source pivot and scale.</param>
        /// <returns><c>false</c> if the animation cannot be retargeted; the error is logged.</returns>
        private bool ExportRetargetedAnimation(ICommandContext commandContext, ContentManager contentManager, Skeleton fileSkeleton, Skeleton targetSkeleton,
            Dictionary<string, AnimationClip> nodeClips, TimeSpan duration, AnimationClip animationClip)
        {
            try
            {
                RetargetAnimation(contentManager.Load<Skeleton>(Retarget.SourceSkeletonUrl), fileSkeleton, targetSkeleton, nodeClips, duration, animationClip);
                return true;
            }
            catch (InvalidOperationException e)
            {
                commandContext.Logger.Error($"The animation {SourcePath} cannot be retargeted: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adds to <paramref name="animationClip"/> the curves of the target skeleton nodes, retargeted from the node curves of the file.
        /// </summary>
        /// <param name="sourceSkeleton">The compiled skeleton the animation was made for, in its rest pose.</param>
        /// <param name="fileSkeleton">The nodes of the animation file, adjusted with the source pivot and scale.</param>
        /// <param name="targetSkeleton">The compiled skeleton to animate.</param>
        /// <param name="nodeClips">The curves of the animation file, one clip per node.</param>
        /// <param name="duration">The duration of the animation.</param>
        /// <param name="animationClip">Receives the curves.</param>
        /// <exception cref="InvalidOperationException">The skeletons or the file cannot be retargeted.</exception>
        internal void RetargetAnimation(Skeleton sourceSkeleton, Skeleton fileSkeleton, Skeleton targetSkeleton, Dictionary<string, AnimationClip> nodeClips, TimeSpan duration, AnimationClip animationClip)
        {
            // The file may lack optional bones of its source skeleton (toes, eyes, fingers of another export): they are left unmapped
            var sourceBones = Retarget.SourceBones
                .Where(x => HumanoidBones.Required.Contains(x.Bone) || fileSkeleton.Nodes.Any(node => node.Name == x.NodeName))
                .ToList();

            // The file may play another body than the source skeleton's, such as another performer of the same capture tool
            var retargeter = new HumanoidRetargeter(sourceSkeleton, sourceBones, targetSkeleton, Retarget.TargetBones, fileSkeleton);

            // Nodes of the file that play the source bones, found by name
            var boneFileNodes = new int[HumanoidBones.Count];
            Array.Fill(boneFileNodes, -1);
            for (var bone = 0; bone < HumanoidBones.Count; ++bone)
            {
                var sourceNode = retargeter.GetSourceNode((HumanoidBone)bone);
                if (sourceNode < 0)
                    continue;
                var nodeName = sourceSkeleton.Nodes[sourceNode].Name;
                boneFileNodes[bone] = Array.FindIndex(fileSkeleton.Nodes, x => x.Name == nodeName);
                if (boneFileNodes[bone] < 0)
                    throw new InvalidOperationException($"The file has no node '{nodeName}', which the source skeleton maps to {(HumanoidBone)bone}.");
            }

            // Transform curves of each node of the file
            var fileNodes = fileSkeleton.Nodes;
            var positionCurves = new AnimationCurve<Vector3>[fileNodes.Length];
            var rotationCurves = new AnimationCurve<Quaternion>[fileNodes.Length];
            var scaleCurves = new AnimationCurve<Vector3>[fileNodes.Length];
            var keyTimes = new SortedSet<CompressedTimeSpan> { CompressedTimeSpan.Zero, (CompressedTimeSpan)duration };
            foreach (var (clipNodeName, clip) in nodeClips)
            {
                var node = Array.FindIndex(fileNodes, x => x.Name == ToNodeName(clipNodeName));
                if (node < 0)
                    continue;
                foreach (var (channelName, channel) in clip.Channels)
                {
                    var curve = clip.Curves[channel.CurveIndex];
                    switch (channelName)
                    {
                        case "Transform.Position": positionCurves[node] = (AnimationCurve<Vector3>)curve; break;
                        case "Transform.Rotation": rotationCurves[node] = (AnimationCurve<Quaternion>)curve; break;
                        case "Transform.Scale": scaleCurves[node] = (AnimationCurve<Vector3>)curve; break;
                        default: continue;
                    }
                    foreach (var time in curve.Keys)
                    {
                        if (time >= CompressedTimeSpan.Zero && time <= (CompressedTimeSpan)duration)
                            keyTimes.Add(time);
                    }
                }
            }

            var targetNodes = retargeter.AnimatedTargetNodes;
            var hipsNode = retargeter.TargetHipsNode;

            // Target nodes that no bone maps (props, tails, twist bones) play the file node of the same name, as an import without
            // retargeting does, but move by its displacement from rest, so they keep the target's bone lengths. The source's humanoid
            // bones move only through the retargeting, and the ancestors of the hips, which carry the root, stay at rest
            var sourceBoneNames = sourceBones.Where(x => !string.IsNullOrEmpty(x.NodeName)).Select(x => x.NodeName).ToHashSet();
            var hipsAncestors = new HashSet<int>();
            for (var node = targetSkeleton.Nodes[hipsNode].ParentIndex; node >= 0; node = targetSkeleton.Nodes[node].ParentIndex)
                hipsAncestors.Add(node);
            var copiedNodes = new List<(int Node, int FileNode, Vector3 SourceRestPosition)>();
            for (var node = 0; node < targetSkeleton.Nodes.Length; ++node)
            {
                var name = targetSkeleton.Nodes[node].Name;
                if (targetNodes.Contains(node) || hipsAncestors.Contains(node) || sourceBoneNames.Contains(name))
                    continue;
                var fileNode = Array.FindIndex(fileNodes, x => x.Name == name);
                if (fileNode < 0 || (positionCurves[fileNode] == null && rotationCurves[fileNode] == null && scaleCurves[fileNode] == null))
                    continue;
                var sourceNode = Array.FindIndex(sourceSkeleton.Nodes, x => x.Name == name);
                copiedNodes.Add((node, fileNode, (sourceNode >= 0 ? sourceSkeleton.Nodes[sourceNode] : fileNodes[fileNode]).Transform.Position));
            }
            var unmappedLocals = targetSkeleton.Nodes.Select(x => x.Transform).ToArray();
            var copiedPositionOut = copiedNodes.Where(x => positionCurves[x.FileNode] != null)
                .ToDictionary(x => x.Node, x => new AnimationCurve<Vector3> { InterpolationType = positionCurves[x.FileNode].InterpolationType });
            var copiedRotationOut = copiedNodes.Where(x => rotationCurves[x.FileNode] != null).ToDictionary(x => x.Node, _ => new AnimationCurve<Quaternion>());
            var copiedScaleOut = copiedNodes.Where(x => scaleCurves[x.FileNode] != null)
                .ToDictionary(x => x.Node, x => new AnimationCurve<Vector3> { InterpolationType = scaleCurves[x.FileNode].InterpolationType });

            var rotationOut = targetNodes.ToDictionary(x => x, _ => new AnimationCurve<Quaternion>());
            var hipsPositionOut = new AnimationCurve<Vector3> { InterpolationType = AnimationCurveInterpolationType.Cubic };
            var rootPositionOut = new AnimationCurve<Vector3> { InterpolationType = AnimationCurveInterpolationType.Cubic };
            var rootRotationOut = new AnimationCurve<Quaternion>();

            var fileLocals = new TransformTRS[fileNodes.Length];
            var fileWorld = new Matrix[fileNodes.Length];
            var boneWorld = new Matrix[HumanoidBones.Count];
            var targetLocals = new TransformTRS[targetSkeleton.Nodes.Length];
            foreach (var time in keyTimes)
            {
                for (var i = 0; i < fileNodes.Length; ++i)
                {
                    var local = fileNodes[i].Transform;
                    if (positionCurves[i] is { } positionCurve)
                    {
                        local.Position = Sample(positionCurve, time, Vector3.Lerp);
                        if (fileNodes[i].ParentIndex == 0)
                            local.Position -= Retarget.SourcePivotPosition;
                        local.Position *= Retarget.SourceScaleImport;
                    }
                    if (rotationCurves[i] is { } rotationCurve)
                        local.Rotation = Sample(rotationCurve, time, (a, b, amount) => Quaternion.Slerp(a, b, amount));
                    if (scaleCurves[i] is { } scaleCurve)
                        local.Scale = Sample(scaleCurve, time, Vector3.Lerp);
                    fileLocals[i] = local;
                }
                HumanoidRetargeter.ComputeWorldMatrices(fileNodes, fileLocals, fileWorld);
                for (var bone = 0; bone < HumanoidBones.Count; ++bone)
                    boneWorld[bone] = boneFileNodes[bone] >= 0 ? fileWorld[boneFileNodes[bone]] : default;

                foreach (var (node, fileNode, sourceRestPosition) in copiedNodes)
                {
                    if (positionCurves[fileNode] != null)
                        unmappedLocals[node].Position = targetSkeleton.Nodes[node].Transform.Position + fileLocals[fileNode].Position - sourceRestPosition;
                    if (rotationCurves[fileNode] != null)
                        unmappedLocals[node].Rotation = fileLocals[fileNode].Rotation;
                    if (scaleCurves[fileNode] != null)
                        unmappedLocals[node].Scale = fileLocals[fileNode].Scale;
                }

                retargeter.Retarget(boneWorld, targetLocals, AnimationRootMotion, out var root, unmappedLocals);

                foreach (var node in targetNodes)
                    AddRotationKey(rotationOut[node], time, targetLocals[node].Rotation);
                foreach (var (node, curve) in copiedPositionOut)
                    curve.KeyFrames.Add(new KeyFrameData<Vector3>(time, targetLocals[node].Position));
                foreach (var (node, curve) in copiedRotationOut)
                    AddRotationKey(curve, time, targetLocals[node].Rotation);
                foreach (var (node, curve) in copiedScaleOut)
                    curve.KeyFrames.Add(new KeyFrameData<Vector3>(time, targetLocals[node].Scale));
                hipsPositionOut.KeyFrames.Add(new KeyFrameData<Vector3>(time, targetLocals[hipsNode].Position));
                if (AnimationRootMotion)
                {
                    rootPositionOut.KeyFrames.Add(new KeyFrameData<Vector3>(time, root.Position));
                    AddRotationKey(rootRotationOut, time, root.Rotation);
                }
            }

            static string NodeChannel(int node, string member) => $"[ModelComponent.Key].Skeleton.NodeTransformations[{node}].{nameof(ModelNodeTransformation.Transform)}.{member}";
            foreach (var (node, curve) in rotationOut.Concat(copiedRotationOut))
                animationClip.AddCurve(NodeChannel(node, nameof(TransformTRS.Rotation)), curve);
            animationClip.AddCurve(NodeChannel(hipsNode, nameof(TransformTRS.Position)), hipsPositionOut);
            foreach (var (node, curve) in copiedPositionOut)
                animationClip.AddCurve(NodeChannel(node, nameof(TransformTRS.Position)), curve);
            foreach (var (node, curve) in copiedScaleOut)
                animationClip.AddCurve(NodeChannel(node, nameof(TransformTRS.Scale)), curve);
            if (AnimationRootMotion)
            {
                animationClip.AddCurve($"[TransformComponent.Key].{nameof(TransformTRS.Position)}", rootPositionOut);
                animationClip.AddCurve($"[TransformComponent.Key].{nameof(TransformTRS.Rotation)}", rootRotationOut);
            }
        }

        /// <summary>
        /// Adds a rotation key on the same hemisphere as the previous one, so interpolation takes the short way.
        /// </summary>
        private static void AddRotationKey(AnimationCurve<Quaternion> curve, CompressedTimeSpan time, Quaternion rotation)
        {
            if (curve.KeyFrames.Count > 0 && Quaternion.Dot(curve.KeyFrames[^1].Value, rotation) < 0)
                rotation = -rotation;
            curve.KeyFrames.Add(new KeyFrameData<Quaternion>(time, rotation));
        }

        /// <summary>
        /// Gets the value of a curve at a time. Between two keys the value is interpolated linearly: the curves of a file usually share their key times.
        /// </summary>
        internal static T Sample<T>(AnimationCurve<T> curve, CompressedTimeSpan time, Func<T, T, float, T> interpolate)
        {
            var keys = curve.KeyFrames;
            if (keys.Count == 0)
                return default;
            var index = curve.FindKeyIndex(time);
            if (index >= keys.Count)
                return keys[^1].Value;
            if (index == 0 || keys[index].Time == time)
                return keys[index].Value;
            var (previous, next) = (keys[index - 1], keys[index]);
            return interpolate(previous.Value, next.Value, (time - previous.Time).Ticks / (float)(next.Time - previous.Time).Ticks);
        }

        /// <summary>
        /// Gets the skeleton node name of a node clip of the animation file.
        /// </summary>
        private static string ToNodeName(string clipNodeName)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                clipNodeName = clipNodeName.Replace(c, '_');
            return clipNodeName;
        }
    }
}
