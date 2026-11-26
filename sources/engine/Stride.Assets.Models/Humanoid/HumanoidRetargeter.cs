// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Assets.Models.Humanoid;

/// <summary>
/// Moves a pose of one humanoid skeleton onto another one, through the standard bones both skeletons map.
/// </summary>
/// <remarks>
/// <para>
/// Each skeleton is first seen in a character frame built from its rest pose (up from the hips to the head, right from the left
/// to the right leg), so skeletons that are Z-up or face another axis behave the same. In that frame, each bone has an alignment:
/// the rotation that turns its rest direction onto a canonical one (arms out sideways, legs down, spine up).
/// The motion of a bone is its rotation away from that canonical pose, and the same motion is applied to the target bone.
/// Rest poses that differ (a T-pose and an A-pose) therefore give the same result.
/// </para>
/// <para>
/// Directions come only from bones both skeletons map. A bone that one skeleton has and the other does not (a neck, fingers)
/// takes the alignment of its nearest ancestor both map, so it does not tilt the bones around it.
/// </para>
/// <para>
/// Feet are the exception: a rest pose stands on flat feet whatever the angle from the ankle to the toes, so a foot keeps its
/// rest pitch. When both skeletons map the toes of a foot, the foot is also turned to face forward; otherwise it moves from its rest.
/// </para>
/// <para>
/// Only the hips move: their displacement from rest is scaled by the ratio of the leg lengths. Target nodes that no bone maps
/// keep their rest transform, or take the one the caller gives them.
/// </para>
/// <para>
/// The animation may play a body of other proportions than the source skeleton, such as another performer of the same capture tool.
/// The source rest pose then takes the bone lengths of that body, so the hips stand as high above the feet as that body's do.
/// </para>
/// </remarks>
internal sealed class HumanoidRetargeter
{
    /// <summary>
    /// The version of the retargeting result, in the build cache key of retargeted animations: increase it every time the result changes.
    /// </summary>
    internal const int Version = 3;

    private static readonly Vector3 CanonicalUp = Vector3.UnitY;
    private static readonly Vector3 CanonicalRight = Vector3.UnitX;
    private static readonly Vector3 CanonicalForward = -Vector3.UnitZ;

    private readonly Rig source;
    private readonly Rig target;
    private readonly Vector3 sourceRestHips;
    private readonly float legRatio;

    private readonly Quaternion[] motion = new Quaternion[HumanoidBones.Count];
    private readonly Quaternion?[] targetWorldRotations;
    private readonly Quaternion[] worldRotations;
    private readonly Matrix[] worldMatrices;

    /// <summary>
    /// Initializes a new instance of the <see cref="HumanoidRetargeter"/> class.
    /// </summary>
    /// <param name="sourceSkeleton">The skeleton the animation was made for, in its rest pose.</param>
    /// <param name="sourceBones">The humanoid bones of the source skeleton.</param>
    /// <param name="targetSkeleton">The skeleton to animate, in its rest pose.</param>
    /// <param name="targetBones">The humanoid bones of the target skeleton.</param>
    /// <param name="sourceBody">
    /// The skeleton the animation plays, when its proportions may differ from <paramref name="sourceSkeleton"/>; <c>null</c> when it is the same body.
    /// Only the lengths of its hips and leg bones are read, which do not depend on its pose, through the node names <paramref name="sourceBones"/> maps.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// A skeleton misses a required bone, maps a node it or <paramref name="sourceBody"/> does not have, or has a rest pose with no clear up and side.
    /// </exception>
    public HumanoidRetargeter(Skeleton sourceSkeleton, IEnumerable<HumanoidBoneMapping> sourceBones, Skeleton targetSkeleton, IEnumerable<HumanoidBoneMapping> targetBones, Skeleton sourceBody = null)
    {
        ArgumentNullException.ThrowIfNull(sourceBones);
        ArgumentNullException.ThrowIfNull(targetBones);
        var sourceMappings = sourceBones.ToList();
        var targetMappings = targetBones.ToList();

        // Both skeletons measure each motion from the same canonical pose only through the bones they both map
        static HashSet<HumanoidBone> Mapped(List<HumanoidBoneMapping> mappings) => mappings.Where(x => !string.IsNullOrEmpty(x.NodeName)).Select(x => x.Bone).ToHashSet();
        var sharedBones = Mapped(sourceMappings);
        sharedBones.IntersectWith(Mapped(targetMappings));

        source = new Rig(sourceSkeleton, sourceMappings, "source", sharedBones);
        target = new Rig(targetSkeleton, targetMappings, "target", sharedBones);

        sourceRestHips = source.RestPosition(HumanoidBone.Hips);
        var sourceLegLength = source.LegLength;
        if (sourceBody != null)
        {
            // The source rest pose with the bone lengths of the body: the hips stand that much higher or lower above the feet
            var body = source.GetBodyPositions(sourceBody);
            var up = Vector3.Transform(CanonicalUp, source.ToWorld);
            sourceRestHips += (source.GetHipsHeight(body, up) - source.GetHipsHeight(source.RestPosition, up)) * up;
            sourceLegLength = Rig.GetLegLength(body);
            if (sourceLegLength <= 1e-6f)
                throw new InvalidOperationException("The animation has legs of length zero.");
        }
        legRatio = target.LegLength / sourceLegLength;

        var nodeCount = target.Nodes.Length;
        targetWorldRotations = new Quaternion?[nodeCount];
        worldRotations = new Quaternion[nodeCount];
        worldMatrices = new Matrix[nodeCount];

        AnimatedTargetNodes = Enumerable.Range(0, HumanoidBones.Count).Select(x => target.BoneNodes[x]).Where(x => x >= 0).Order().ToList();
    }

    /// <summary>
    /// Gets the target nodes that <see cref="Retarget"/> rotates, in node order.
    /// </summary>
    public IReadOnlyList<int> AnimatedTargetNodes { get; }

    /// <summary>
    /// Gets the target node of the hips, the only node <see cref="Retarget"/> also moves.
    /// </summary>
    public int TargetHipsNode => target.BoneNodes[(int)HumanoidBone.Hips];

    /// <summary>
    /// Gets the index of the source skeleton node that plays a bone.
    /// </summary>
    /// <param name="bone">The bone.</param>
    /// <returns>The node index, or -1 when the source skeleton does not map the bone.</returns>
    public int GetSourceNode(HumanoidBone bone) => source.BoneNodes[(int)bone];

    /// <summary>
    /// Computes the model-space matrices of a pose.
    /// </summary>
    /// <param name="nodes">The nodes of the skeleton, parents first.</param>
    /// <param name="localTransforms">The local transform of each node.</param>
    /// <param name="worldMatrices">Receives the model-space matrix of each node.</param>
    public static void ComputeWorldMatrices(ModelNodeDefinition[] nodes, ReadOnlySpan<TransformTRS> localTransforms, Span<Matrix> worldMatrices)
    {
        for (var i = 0; i < nodes.Length; ++i)
        {
            var local = localTransforms[i];
            Matrix.Transformation(ref local.Scale, ref local.Rotation, ref local.Position, out var matrix);
            var parent = nodes[i].ParentIndex;
            worldMatrices[i] = parent >= 0 ? Matrix.Multiply(matrix, worldMatrices[parent]) : matrix;
        }
    }

    /// <summary>
    /// Moves a pose of the source skeleton onto the target skeleton.
    /// </summary>
    /// <param name="sourceBoneWorld">The model-space matrix of each source bone, indexed by <see cref="HumanoidBone"/>. Only the bones the source maps are read.</param>
    /// <param name="targetLocal">Receives the local transform of each target node.</param>
    /// <param name="rootMotion">
    /// <c>true</c> to take the horizontal displacement and the turn of the hips out of the skeleton, into <paramref name="root"/>;
    /// <c>false</c> to keep all the motion in the skeleton.
    /// </param>
    /// <param name="root">The transform of the model when <paramref name="rootMotion"/> is <c>true</c>, otherwise the identity.</param>
    /// <param name="unmappedLocal">
    /// The local transform of each target node, read only for the nodes no bone maps; empty to keep them at rest.
    /// The mapped nodes below such a node keep their world rotation.
    /// </param>
    public void Retarget(ReadOnlySpan<Matrix> sourceBoneWorld, Span<TransformTRS> targetLocal, bool rootMotion, out TransformTRS root, ReadOnlySpan<TransformTRS> unmappedLocal = default)
    {
        // Motion of each bone away from the canonical pose; a bone the source does not map moves with its parent
        for (var bone = 0; bone < HumanoidBones.Count; ++bone)
        {
            if (source.BoneNodes[bone] >= 0)
            {
                sourceBoneWorld[bone].Decompose(out _, out Quaternion worldRotation, out _);
                motion[bone] = Then(source.FromRestCanonical[bone], Then(worldRotation, source.ToCharacter));
            }
            else
            {
                motion[bone] = motion[(int)HumanoidBones.GetParent((HumanoidBone)bone)!.Value];
            }
        }

        Array.Clear(targetWorldRotations);
        for (var bone = 0; bone < HumanoidBones.Count; ++bone)
        {
            var node = target.BoneNodes[bone];
            if (node >= 0)
                targetWorldRotations[node] = Then(Then(target.RestCanonical[bone], motion[bone]), target.ToWorld);
        }

        // Hips displacement from rest, in the character frame, scaled to the target's legs
        var hips = (int)HumanoidBone.Hips;
        var hipsNode = target.BoneNodes[hips];
        var sourceDisplacement = Vector3.Transform(sourceBoneWorld[hips].TranslationVector - sourceRestHips, source.ToCharacter);
        var hipsDisplacement = Vector3.Transform(sourceDisplacement * legRatio, target.ToWorld);
        var hipsPosition = target.RestPosition(HumanoidBone.Hips) + hipsDisplacement;

        root = new TransformTRS { Rotation = Quaternion.Identity, Scale = Vector3.One };
        if (rootMotion)
        {
            // The model transform carries the root: the skeleton keeps what is left, in the model's space
            SplitRootMotion(motion[hips], hipsDisplacement, out root.Position, out root.Rotation);
            var inverseRootRotation = Quaternion.Invert(root.Rotation);
            hipsPosition = Vector3.Transform(hipsPosition - root.Position, inverseRootRotation);
            foreach (var node in AnimatedTargetNodes)
                targetWorldRotations[node] = Then(targetWorldRotations[node]!.Value, inverseRootRotation);
        }

        // Local transforms, parents first: mapped nodes get their world rotation, the others keep their rest or take the given one
        for (var i = 0; i < target.Nodes.Length; ++i)
        {
            var parent = target.Nodes[i].ParentIndex;
            var parentRotation = parent >= 0 ? worldRotations[parent] : Quaternion.Identity;
            var local = targetWorldRotations[i] == null && !unmappedLocal.IsEmpty ? unmappedLocal[i] : target.Nodes[i].Transform;
            if (targetWorldRotations[i] is { } worldRotation)
            {
                local.Rotation = Quaternion.Normalize(Then(worldRotation, Quaternion.Invert(parentRotation)));
                worldRotations[i] = worldRotation;
            }
            else
            {
                worldRotations[i] = Then(local.Rotation, parentRotation);
            }

            if (i == hipsNode)
                local.Position = parent >= 0 ? Vector3.TransformCoordinate(hipsPosition, Matrix.Invert(worldMatrices[parent])) : hipsPosition;

            Matrix.Transformation(ref local.Scale, ref local.Rotation, ref local.Position, out var matrix);
            worldMatrices[i] = parent >= 0 ? Matrix.Multiply(matrix, worldMatrices[parent]) : matrix;
            targetLocal[i] = local;
        }
    }

    /// <summary>
    /// Splits the hips motion into the part a model transform carries (horizontal displacement and turn around the up axis) and the rest.
    /// </summary>
    private void SplitRootMotion(Quaternion hipsMotion, Vector3 hipsDisplacement, out Vector3 position, out Quaternion rotation)
    {
        var up = Vector3.Transform(CanonicalUp, target.ToWorld);
        position = hipsDisplacement - Vector3.Dot(hipsDisplacement, up) * up;

        // Turn: the canonical forward moved by the hips, flattened on the ground, in the character frame
        var forward = Vector3.Transform(CanonicalForward, hipsMotion);
        forward.Y = 0;
        rotation = Quaternion.Identity;
        if (forward.LengthSquared() > 1e-8f)
        {
            forward.Normalize();
            var turn = Quaternion.RotationAxis(CanonicalUp, MathF.Atan2(-forward.X, -forward.Z));
            rotation = Then(Then(target.ToCharacter, turn), target.ToWorld);
        }
    }

    /// <summary>
    /// Combines two rotations: <paramref name="first"/>, then <paramref name="second"/>.
    /// </summary>
    private static Quaternion Then(Quaternion first, Quaternion second) => Quaternion.Multiply(first, second);

    private static Vector3 GetCanonicalDirection(HumanoidBone bone)
    {
        if (HumanoidBones.IsLeft(bone) || HumanoidBones.IsRight(bone))
        {
            var stem = bone.ToString().AsSpan(HumanoidBones.IsLeft(bone) ? 4 : 5);
            if (stem is "UpperLeg" or "LowerLeg")
                return -CanonicalUp;
            return HumanoidBones.IsLeft(bone) ? -CanonicalRight : CanonicalRight;
        }
        return CanonicalUp;
    }

    /// <summary>
    /// Gets the bones whose position, seen from a bone, gives its rest direction, in order of preference.
    /// </summary>
    private static IEnumerable<HumanoidBone> GetDirectionChildren(HumanoidBone bone)
    {
        HumanoidBone[] spine = [HumanoidBone.Spine, HumanoidBone.Chest, HumanoidBone.UpperChest, HumanoidBone.Neck, HumanoidBone.Head];
        switch (bone)
        {
            case <= HumanoidBone.Neck:
                return spine[((int)bone)..];
            case HumanoidBone.Head:
                return [];
            case HumanoidBone.LeftHand:
                return [HumanoidBone.LeftMiddleProximal, HumanoidBone.LeftIndexProximal, HumanoidBone.LeftRingProximal, HumanoidBone.LeftLittleProximal];
            case HumanoidBone.RightHand:
                return [HumanoidBone.RightMiddleProximal, HumanoidBone.RightIndexProximal, HumanoidBone.RightRingProximal, HumanoidBone.RightLittleProximal];
            default:
                return Enum.GetValues<HumanoidBone>().Where(x => HumanoidBones.GetParent(x) == bone);
        }
    }

    /// <summary>
    /// A skeleton in its rest pose, with its humanoid bones and character frame.
    /// </summary>
    private sealed class Rig
    {
        public readonly ModelNodeDefinition[] Nodes;
        public readonly int[] BoneNodes = new int[HumanoidBones.Count];
        public readonly Matrix[] RestWorld;
        public readonly Quaternion ToCharacter;
        public readonly Quaternion ToWorld;

        // Rest rotation of each bone in the character frame, followed by its alignment (A·G_rest), and its inverse
        public readonly Quaternion[] RestCanonical = new Quaternion[HumanoidBones.Count];
        public readonly Quaternion[] FromRestCanonical = new Quaternion[HumanoidBones.Count];
        public readonly float LegLength;

        // sharedBones: the bones both skeletons map, the only ones that give directions
        public Rig(Skeleton skeleton, List<HumanoidBoneMapping> bones, string side, IReadOnlySet<HumanoidBone> sharedBones)
        {
            ArgumentNullException.ThrowIfNull(skeleton);
            Nodes = skeleton.Nodes;

            Array.Fill(BoneNodes, -1);
            var mappings = bones.Where(x => !string.IsNullOrEmpty(x.NodeName)).ToList();
            foreach (var mapping in mappings)
            {
                var node = Array.FindIndex(Nodes, x => x.Name == mapping.NodeName);
                if (node < 0)
                    throw new InvalidOperationException($"The {side} skeleton maps {mapping.Bone} to node '{mapping.NodeName}', which is not in the compiled skeleton. Check that the node exists and is preserved.");
                if (BoneNodes[(int)mapping.Bone] >= 0)
                    throw new InvalidOperationException($"The {side} skeleton maps {mapping.Bone} more than once.");
                BoneNodes[(int)mapping.Bone] = node;
            }
            var missing = HumanoidBones.GetMissingRequired(mappings);
            if (missing.Count > 0)
                throw new InvalidOperationException($"The {side} skeleton does not map the required humanoid bones {string.Join(", ", missing)}.");

            RestWorld = new Matrix[Nodes.Length];
            ComputeWorldMatrices(Nodes, Nodes.Select(x => x.Transform).ToArray(), RestWorld);

            // Character frame: up from the hips to the head, right from the left leg to the right leg
            var hips = RestPosition(HumanoidBone.Hips);
            var up = RestPosition(HumanoidBone.Head) - hips;
            var right = RestPosition(HumanoidBone.RightUpperLeg) - RestPosition(HumanoidBone.LeftUpperLeg);
            if (up.LengthSquared() < 1e-12f || Vector3.Dot(RestPosition(HumanoidBone.Spine) - hips, up) < 0)
                throw new InvalidOperationException($"The {side} skeleton has no clear up direction at rest: its {HumanoidBone.Head} and {HumanoidBone.Spine} must be above its {HumanoidBone.Hips}.");
            up.Normalize();
            right -= Vector3.Dot(right, up) * up;
            if (right.LengthSquared() < 1e-12f)
                throw new InvalidOperationException($"The {side} skeleton has no clear side direction at rest: its {HumanoidBone.LeftUpperLeg} and {HumanoidBone.RightUpperLeg} must be side by side.");
            right.Normalize();

            // Rows are the world images of the character axes: right (+X), up (+Y) and back (+Z)
            var characterToWorld = Matrix.Identity;
            characterToWorld.Right = right;
            characterToWorld.Up = up;
            characterToWorld.Backward = Vector3.Cross(right, up);
            ToWorld = Quaternion.Normalize(Quaternion.RotationMatrix(characterToWorld));
            ToCharacter = Quaternion.Invert(ToWorld);

            // Bones in enum order: a parent comes before its children. A bone the other skeleton does not map, or with no direction,
            // takes the alignment of its nearest ancestor both skeletons map
            var alignments = new Quaternion[HumanoidBones.Count];
            var inherited = new Quaternion[HumanoidBones.Count];
            for (var i = 0; i < HumanoidBones.Count; ++i)
            {
                var bone = (HumanoidBone)i;
                inherited[i] = HumanoidBones.GetParent(bone) is { } parent
                    ? sharedBones.Contains(parent) ? alignments[(int)parent] : inherited[(int)parent]
                    : Quaternion.Identity;
                alignments[i] = sharedBones.Contains(bone) ? GetAlignment(bone, inherited[i], sharedBones) : inherited[i];
                if (BoneNodes[i] < 0)
                    continue;

                RestWorld[BoneNodes[i]].Decompose(out _, out Quaternion restRotation, out _);
                RestCanonical[i] = Then(Then(restRotation, ToCharacter), alignments[i]);
                FromRestCanonical[i] = Quaternion.Invert(RestCanonical[i]);
            }

            LegLength = GetLegLength(RestPosition);
            if (LegLength <= 1e-6f)
                throw new InvalidOperationException($"The {side} skeleton has legs of length zero at rest.");
        }

        public Vector3 RestPosition(HumanoidBone bone) => RestWorld[BoneNodes[(int)bone]].TranslationVector;

        /// <summary>
        /// Gets the position of each mapped bone in another skeleton whose nodes have the names this one maps, in its own pose.
        /// </summary>
        public Func<HumanoidBone, Vector3> GetBodyPositions(Skeleton body)
        {
            var world = new Matrix[body.Nodes.Length];
            ComputeWorldMatrices(body.Nodes, body.Nodes.Select(x => x.Transform).ToArray(), world);
            return bone =>
            {
                var name = Nodes[BoneNodes[(int)bone]].Name;
                var node = Array.FindIndex(body.Nodes, x => x.Name == name);
                if (node < 0)
                    throw new InvalidOperationException($"The animation has no node '{name}', which the source skeleton maps to {bone}.");
                return world[node].TranslationVector;
            };
        }

        /// <summary>
        /// Gets the height of the hips above the lowest joint of the legs, in the rest pose with the bone lengths of a body.
        /// </summary>
        /// <param name="body">The position of each bone of the body; only the distances between joints are read, which do not depend on its pose.</param>
        public float GetHipsHeight(Func<HumanoidBone, Vector3> body, Vector3 up)
        {
            var height = 0f;
            foreach (var leg in new[]
            {
                new[] { HumanoidBone.Hips, HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot, HumanoidBone.LeftToes },
                new[] { HumanoidBone.Hips, HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot, HumanoidBone.RightToes },
            })
            {
                // Down the leg: each segment keeps its rest direction and takes the length it has in the body
                var drop = 0f;
                var lowest = 0f;
                for (var i = 1; i < leg.Length && BoneNodes[(int)leg[i]] >= 0; ++i)
                {
                    var rest = RestPosition(leg[i]) - RestPosition(leg[i - 1]);
                    var length = rest.Length();
                    if (length < 1e-6f)
                        continue;
                    drop += Vector3.Dot(rest, up) / length * Vector3.Distance(body(leg[i]), body(leg[i - 1]));
                    lowest = MathF.Min(lowest, drop);
                }
                height -= lowest;
            }
            return height / 2;
        }

        /// <summary>
        /// Gets the rotation that turns the rest direction of a mapped bone onto its canonical direction, in the character frame.
        /// </summary>
        private Quaternion GetAlignment(HumanoidBone bone, Quaternion parentAlignment, IReadOnlySet<HumanoidBone> sharedBones)
        {
            if (bone is HumanoidBone.LeftFoot or HumanoidBone.RightFoot)
            {
                // Only the heading, around the up axis: whatever the angle from the ankle to the toes, a foot at rest is flat on the ground.
                // A foot faces where its toes point, so it is turned only when both skeletons map them
                var toes = bone == HumanoidBone.LeftFoot ? HumanoidBone.LeftToes : HumanoidBone.RightToes;
                if (!sharedBones.Contains(toes))
                    return Quaternion.Identity;
                var heading = Vector3.Transform(RestPosition(toes) - RestPosition(bone), ToCharacter);
                heading.Y = 0;
                return heading.LengthSquared() > 1e-12f ? Quaternion.BetweenDirections(Vector3.Normalize(heading), CanonicalForward) : Quaternion.Identity;
            }

            foreach (var child in GetDirectionChildren(bone))
            {
                if (!sharedBones.Contains(child))
                    continue;
                var direction = Vector3.Transform(RestPosition(child) - RestPosition(bone), ToCharacter);
                if (direction.LengthSquared() < 1e-12f)
                    continue;
                return Quaternion.BetweenDirections(Vector3.Normalize(direction), GetCanonicalDirection(bone));
            }
            return parentAlignment;
        }

        // The mean of both legs, from the upper leg to the foot: joint distances, which do not depend on the pose
        public static float GetLegLength(Func<HumanoidBone, Vector3> position)
        {
            float Leg(HumanoidBone upper, HumanoidBone lower, HumanoidBone foot)
                => Vector3.Distance(position(upper), position(lower)) + Vector3.Distance(position(lower), position(foot));
            return (Leg(HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot)
                + Leg(HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot)) / 2;
        }
    }
}
