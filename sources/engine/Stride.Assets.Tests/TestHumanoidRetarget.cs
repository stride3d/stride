// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Models.Humanoid;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// The retargeting math on synthetic skeletons: Y-up, facing -Z, the character's left on -X.
/// </summary>
public class TestHumanoidRetarget
{
    private static readonly HumanoidBone[] Bones =
    [
        HumanoidBone.Hips, HumanoidBone.Spine, HumanoidBone.Chest, HumanoidBone.Neck, HumanoidBone.Head,
        HumanoidBone.LeftShoulder, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand,
        HumanoidBone.RightShoulder, HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand,
        HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot, HumanoidBone.LeftToes,
        HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot, HumanoidBone.RightToes,
    ];

    // Bone and the bone whose position gives its direction
    private static readonly (HumanoidBone Bone, HumanoidBone Child)[] Segments =
    [
        (HumanoidBone.Spine, HumanoidBone.Chest), (HumanoidBone.Neck, HumanoidBone.Head),
        (HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm), (HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand),
        (HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm), (HumanoidBone.RightLowerArm, HumanoidBone.RightHand),
        (HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg), (HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot),
        (HumanoidBone.RightFoot, HumanoidBone.RightToes),
    ];

    /// <summary>
    /// A synthetic humanoid: a "Root" node, one node per bone named after it, and a "Prop" node in the right hand that no bone maps.
    /// </summary>
    private sealed class Rig
    {
        public Skeleton Skeleton;
        public List<HumanoidBoneMapping> Mapping;
        public Dictionary<string, int> NodeIndex;

        public int this[HumanoidBone bone] => NodeIndex[bone.ToString()];

        public int this[string node] => NodeIndex[node];

        public TransformTRS[] RestLocals => Skeleton.Nodes.Select(x => x.Transform).ToArray();

        public Matrix[] World(TransformTRS[] locals)
        {
            var world = new Matrix[locals.Length];
            HumanoidRetargeter.ComputeWorldMatrices(Skeleton.Nodes, locals, world);
            return world;
        }

        public Matrix[] BoneWorld(TransformTRS[] locals)
        {
            var world = World(locals);
            var bones = new Matrix[HumanoidBones.Count];
            foreach (var bone in Bones)
                bones[(int)bone] = world[this[bone]];
            return bones;
        }
    }

    /// <summary>
    /// The T-pose: arms straight out sideways, legs straight down.
    /// </summary>
    private static Vector3 TPose(HumanoidBone bone) => bone switch
    {
        HumanoidBone.Hips => new(0, 1, 0),
        HumanoidBone.Spine => new(0, 1.1f, 0),
        HumanoidBone.Chest => new(0, 1.3f, 0),
        HumanoidBone.Neck => new(0, 1.5f, 0),
        HumanoidBone.Head => new(0, 1.6f, 0),
        HumanoidBone.LeftShoulder => new(-0.1f, 1.45f, 0),
        HumanoidBone.LeftUpperArm => new(-0.2f, 1.45f, 0),
        HumanoidBone.LeftLowerArm => new(-0.5f, 1.45f, 0),
        HumanoidBone.LeftHand => new(-0.75f, 1.45f, 0),
        HumanoidBone.LeftUpperLeg => new(-0.1f, 0.95f, 0),
        HumanoidBone.LeftLowerLeg => new(-0.1f, 0.5f, 0),
        HumanoidBone.LeftFoot => new(-0.1f, 0.1f, 0),
        HumanoidBone.LeftToes => new(-0.1f, 0.02f, -0.15f),
        _ when HumanoidBones.IsRight(bone) => Mirror(TPose(Enum.Parse<HumanoidBone>("Left" + bone.ToString()[5..]))),
        _ => throw new ArgumentOutOfRangeException(nameof(bone)),
    };

    /// <summary>
    /// The A-pose: the T-pose with each arm turned 45° down at the upper arm.
    /// </summary>
    private static Vector3 APose(HumanoidBone bone)
    {
        var position = TPose(bone);
        if (bone is HumanoidBone.LeftLowerArm or HumanoidBone.LeftHand or HumanoidBone.RightLowerArm or HumanoidBone.RightHand)
        {
            var upperArm = TPose(HumanoidBones.IsLeft(bone) ? HumanoidBone.LeftUpperArm : HumanoidBone.RightUpperArm);
            var length = Vector3.Distance(position, upperArm);
            position = upperArm + length * Vector3.Normalize(new Vector3(MathF.Sign(position.X - upperArm.X), -1, 0));
        }
        return position;
    }

    private static Vector3 Mirror(Vector3 v) => new(-v.X, v.Y, v.Z);

    /// <summary>
    /// Builds a rig from the model-space rest position and rotation of each bone.
    /// </summary>
    private static Rig BuildRig(Func<HumanoidBone, Vector3> position, Func<HumanoidBone, Quaternion> rotation = null, Matrix? placement = null)
    {
        rotation ??= _ => Quaternion.Identity;
        var place = placement ?? Matrix.Identity;

        var names = new List<string> { "Root" };
        var parents = new List<int> { -1 };
        var worlds = new List<Matrix> { place };
        foreach (var bone in Bones)
        {
            var parent = HumanoidBones.GetParent(bone);
            while (parent is { } p && !Bones.Contains(p))
                parent = HumanoidBones.GetParent(p);
            names.Add(bone.ToString());
            parents.Add(parent is { } mapped ? names.IndexOf(mapped.ToString()) : 0);
            worlds.Add(Matrix.RotationQuaternion(rotation(bone)) * Matrix.Translation(position(bone)) * place);
        }
        names.Add("Prop");
        parents.Add(names.IndexOf(nameof(HumanoidBone.RightHand)));
        worlds.Add(Matrix.Translation(position(HumanoidBone.RightHand) + new Vector3(0.1f, 0, 0)) * place);

        var nodes = new ModelNodeDefinition[names.Count];
        for (var i = 0; i < nodes.Length; ++i)
        {
            var local = parents[i] >= 0 ? worlds[i] * Matrix.Invert(worlds[parents[i]]) : worlds[i];
            local.Decompose(out var scale, out Quaternion localRotation, out var translation);
            nodes[i] = new ModelNodeDefinition { Name = names[i], ParentIndex = parents[i], Transform = new TransformTRS { Position = translation, Rotation = localRotation, Scale = Vector3.One } };
        }

        return new Rig
        {
            Skeleton = new Skeleton { Nodes = nodes },
            Mapping = Bones.Select(x => new HumanoidBoneMapping { Bone = x, NodeName = x.ToString() }).ToList(),
            NodeIndex = names.Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i),
        };
    }

    /// <summary>
    /// A pose with every bone turned a little, and the hips moved.
    /// </summary>
    private static TransformTRS[] RandomPose(Rig rig, int seed, Vector3 hipsOffset)
    {
        var random = new Random(seed);
        var locals = rig.RestLocals;
        foreach (var bone in Bones)
        {
            var axis = Vector3.Normalize(new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f));
            var turn = Quaternion.RotationAxis(axis, (random.NextSingle() - 0.5f) * 1.5f);
            locals[rig[bone]].Rotation = Quaternion.Normalize(turn * locals[rig[bone]].Rotation);
        }
        locals[rig[HumanoidBone.Hips]].Position += hipsOffset;
        return locals;
    }

    private static TransformTRS[] Retarget(Rig source, Rig target, TransformTRS[] sourcePose, bool rootMotion, out TransformTRS root)
    {
        var retargeter = new HumanoidRetargeter(source.Skeleton, source.Mapping, target.Skeleton, target.Mapping);
        var result = new TransformTRS[target.Skeleton.Nodes.Length];
        retargeter.Retarget(source.BoneWorld(sourcePose), result, rootMotion, out root);
        return result;
    }

    private static TransformTRS[] Retarget(Rig source, Rig target, TransformTRS[] sourcePose) => Retarget(source, target, sourcePose, false, out _);

    private static Vector3 Direction(Matrix[] world, Rig rig, HumanoidBone bone, HumanoidBone child)
        => Vector3.Normalize(world[rig[child]].TranslationVector - world[rig[bone]].TranslationVector);

    private static void AssertClose(Vector3 expected, Vector3 actual, float tolerance)
        => Assert.True((expected - actual).Length() < tolerance, $"Expected {expected}, got {actual}");

    /// <summary>
    /// Checks that two rotations are the same within a tolerance on each component (<c>q</c> and <c>-q</c> are the same rotation).
    /// </summary>
    internal static void AssertClose(Quaternion expected, Quaternion actual, float tolerance, string context = null)
    {
        var sign = Quaternion.Dot(expected, actual) < 0 ? -1 : 1;
        var error = MathF.Max(MathF.Max(MathF.Abs(expected.X - sign * actual.X), MathF.Abs(expected.Y - sign * actual.Y)),
            MathF.Max(MathF.Abs(expected.Z - sign * actual.Z), MathF.Abs(expected.W - sign * actual.W)));
        Assert.True(error < tolerance, $"{context} Expected {expected}, got {actual}");
    }

    [Fact]
    public void SameSkeletonGivesTheInputBack()
    {
        var rig = BuildRig(TPose);
        var pose = RandomPose(rig, 1, new Vector3(0.1f, -0.05f, 0.2f));

        var result = Retarget(rig, rig, pose);

        foreach (var bone in Bones)
            AssertClose(pose[rig[bone]].Rotation, result[rig[bone]].Rotation, 1e-4f);
        AssertClose(pose[rig[HumanoidBone.Hips]].Position, result[rig[HumanoidBone.Hips]].Position, 1e-4f);
    }

    [Fact]
    public void TPoseSourceStraightensAPoseTarget()
    {
        var source = BuildRig(TPose);
        var target = BuildRig(APose);

        // Source at rest: the target arms go horizontal
        var world = target.World(Retarget(source, target, source.RestLocals));
        AssertClose(-Vector3.UnitX, Direction(world, target, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm), 1e-4f);
        AssertClose(Vector3.UnitX, Direction(world, target, HumanoidBone.RightLowerArm, HumanoidBone.RightHand), 1e-4f);
        AssertClose(-Vector3.UnitY, Direction(world, target, HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg), 1e-4f);

        // Source left arm raised straight up: so is the target's
        var raised = source.RestLocals;
        raised[source[HumanoidBone.LeftUpperArm]].Rotation = Quaternion.BetweenDirections(-Vector3.UnitX, Vector3.UnitY);
        world = target.World(Retarget(source, target, raised));
        AssertClose(Vector3.UnitY, Direction(world, target, HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm), 1e-4f);
        AssertClose(Vector3.UnitY, Direction(world, target, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand), 1e-4f);
    }

    [Fact]
    public void OtherBoneAxesAndZUpGiveTheSameDirections()
    {
        var source = BuildRig(TPose);

        // Each bone frame turned 90° around the bone, and the whole skeleton Z-up and facing +X
        Quaternion Twist(HumanoidBone bone)
        {
            var child = Segments.FirstOrDefault(x => x.Bone == bone).Child;
            var axis = child != default ? Vector3.Normalize(TPose(child) - TPose(bone)) : Vector3.UnitY;
            return Quaternion.RotationAxis(axis, MathUtil.PiOverTwo);
        }
        var placement = Matrix.RotationX(MathUtil.PiOverTwo) * Matrix.RotationZ(MathUtil.PiOverTwo);
        var target = BuildRig(TPose, Twist, placement);

        var pose = RandomPose(source, 2, Vector3.Zero);
        var sourceWorld = source.World(pose);
        var targetWorld = target.World(Retarget(source, target, pose));

        var toSourceSpace = Matrix.Invert(placement);
        foreach (var (bone, child) in Segments)
        {
            var targetDirection = Vector3.TransformNormal(Direction(targetWorld, target, bone, child), toSourceSpace);
            AssertClose(Direction(sourceWorld, source, bone, child), targetDirection, 1e-3f);
        }
    }

    [Fact]
    public void FootWithoutToesKeepsItsRestPose()
    {
        var source = BuildRig(TPose);

        // The target has no toe bones and its feet are tilted at rest
        var tilt = Quaternion.RotationX(-0.3f);
        var target = BuildRig(TPose, x => x is HumanoidBone.LeftFoot or HumanoidBone.RightFoot ? tilt : Quaternion.Identity);
        target.Mapping.RemoveAll(x => x.Bone is HumanoidBone.LeftToes or HumanoidBone.RightToes);

        var world = target.World(Retarget(source, target, source.RestLocals));

        foreach (var foot in new[] { HumanoidBone.LeftFoot, HumanoidBone.RightFoot })
        {
            world[target[foot]].Decompose(out _, out Quaternion rotation, out _);
            AssertClose(tilt, rotation, 1e-4f, foot.ToString());
        }
    }

    [Fact]
    public void FeetKeepTheirRestPitchAndFaceForward()
    {
        // The source toes point down and forward; the target toes are level and turned out
        var source = BuildRig(TPose);
        var target = BuildRig(x => x == HumanoidBone.LeftToes ? new Vector3(-0.15f, 0.1f, -0.15f)
            : x == HumanoidBone.RightToes ? new Vector3(0.15f, 0.1f, -0.15f) : TPose(x));

        // Source at rest: the target feet stay level and face forward
        var world = target.World(Retarget(source, target, source.RestLocals));
        AssertClose(-Vector3.UnitZ, Direction(world, target, HumanoidBone.LeftFoot, HumanoidBone.LeftToes), 1e-4f);
        AssertClose(-Vector3.UnitZ, Direction(world, target, HumanoidBone.RightFoot, HumanoidBone.RightToes), 1e-4f);

        // Source left toes raised: the target ones rise as much
        var raise = Quaternion.RotationX(0.4f);
        var raised = source.RestLocals;
        raised[source[HumanoidBone.LeftFoot]].Rotation = raise;
        world = target.World(Retarget(source, target, raised));
        AssertClose(Vector3.Transform(-Vector3.UnitZ, raise), Direction(world, target, HumanoidBone.LeftFoot, HumanoidBone.LeftToes), 1e-4f);
    }

    [Fact]
    public void BoneOnlyOneSkeletonMapsDoesNotTiltTheOthers()
    {
        // The head is forward of the neck at rest: chest to head and neck to head point different ways
        static Vector3 Leaning(HumanoidBone bone) => bone == HumanoidBone.Head ? new Vector3(0, 1.6f, -0.06f) : TPose(bone);
        var pose = RandomPose(BuildRig(Leaning), 5, Vector3.Zero);

        foreach (var neckOnSource in new[] { true, false })
        {
            var source = BuildRig(Leaning);
            var target = BuildRig(Leaning);
            (neckOnSource ? target : source).Mapping.RemoveAll(x => x.Bone == HumanoidBone.Neck);

            var sourceWorld = source.World(pose);
            var targetWorld = target.World(Retarget(source, target, pose));
            foreach (var bone in new[] { HumanoidBone.Chest, HumanoidBone.Head, HumanoidBone.LeftUpperArm })
            {
                sourceWorld[source[bone]].Decompose(out _, out Quaternion expected, out _);
                targetWorld[target[bone]].Decompose(out _, out Quaternion actual, out _);
                AssertClose(expected, actual, 1e-4f, $"{bone}, neck mapped on the {(neckOnSource ? "source" : "target")} only");
            }
        }
    }

    [Fact]
    public void LongerLegsScaleTheHipsDisplacement()
    {
        var source = BuildRig(TPose);
        var target = BuildRig(x => TPose(x) * 2);
        var offset = new Vector3(0.1f, -0.05f, 0.2f);
        var pose = source.RestLocals;
        pose[source[HumanoidBone.Hips]].Position += offset;

        var world = target.World(Retarget(source, target, pose));

        AssertClose(TPose(HumanoidBone.Hips) * 2 + offset * 2, world[target[HumanoidBone.Hips]].TranslationVector, 1e-4f);
    }

    [Fact]
    public void ShorterBodyOnTheSourceSkeletonStaysOnTheGround()
    {
        // Another performer of the same tool: the source skeleton scaled down about its lowest foot joint, the toes
        var source = BuildRig(TPose);
        var ground = new Vector3(0, TPose(HumanoidBone.LeftToes).Y, 0);
        var body = BuildRig(x => ground + (TPose(x) - ground) * 0.8f);
        var target = BuildRig(TPose);

        // The body at rest: the target stands at rest
        var retargeter = new HumanoidRetargeter(source.Skeleton, source.Mapping, target.Skeleton, target.Mapping, body.Skeleton);
        var result = new TransformTRS[target.Skeleton.Nodes.Length];
        retargeter.Retarget(body.BoneWorld(body.RestLocals), result, false, out _);
        AssertClose(TPose(HumanoidBone.Hips), target.World(result)[target[HumanoidBone.Hips]].TranslationVector, 1e-4f);

        // The body crouches: the target crouches as much, scaled to its longer legs
        var crouch = body.RestLocals;
        crouch[body[HumanoidBone.Hips]].Position += new Vector3(0, -0.1f, 0);
        retargeter.Retarget(body.BoneWorld(crouch), result, false, out _);
        AssertClose(TPose(HumanoidBone.Hips) + new Vector3(0, -0.1f / 0.8f, 0), target.World(result)[target[HumanoidBone.Hips]].TranslationVector, 1e-4f);
    }

    [Fact]
    public void UnmappedTargetNodesStayAtRest()
    {
        var source = BuildRig(TPose);
        var target = BuildRig(APose);
        target.Mapping.RemoveAll(x => x.Bone == HumanoidBone.LeftShoulder);

        var retargeter = new HumanoidRetargeter(source.Skeleton, source.Mapping, target.Skeleton, target.Mapping);
        var result = new TransformTRS[target.Skeleton.Nodes.Length];
        retargeter.Retarget(source.BoneWorld(RandomPose(source, 3, Vector3.Zero)), result, false, out _);

        Assert.DoesNotContain(target["Prop"], retargeter.AnimatedTargetNodes);
        Assert.DoesNotContain(target["Root"], retargeter.AnimatedTargetNodes);
        Assert.DoesNotContain(target[HumanoidBone.LeftShoulder], retargeter.AnimatedTargetNodes);
        Assert.Contains(target[HumanoidBone.LeftUpperArm], retargeter.AnimatedTargetNodes);
        foreach (var node in new[] { target["Prop"], target["Root"], target[HumanoidBone.LeftShoulder] })
            AssertClose(target.Skeleton.Nodes[node].Transform.Rotation, result[node].Rotation, 1e-6f);
    }

    [Fact]
    public void UnmappedTargetNodesTakeTheGivenPose()
    {
        var source = BuildRig(TPose);
        var target = BuildRig(APose);
        target.Mapping.RemoveAll(x => x.Bone == HumanoidBone.LeftShoulder);
        var retargeter = new HumanoidRetargeter(source.Skeleton, source.Mapping, target.Skeleton, target.Mapping);
        var sourceBones = source.BoneWorld(RandomPose(source, 5, Vector3.Zero));

        var atRest = new TransformTRS[target.Skeleton.Nodes.Length];
        retargeter.Retarget(sourceBones, atRest, false, out _);

        // The unmapped shoulder turns, and the prop also moves and grows
        var given = target.RestLocals;
        var shoulder = target[HumanoidBone.LeftShoulder];
        var prop = target["Prop"];
        given[shoulder].Rotation = Quaternion.Normalize(Quaternion.RotationZ(0.4f) * given[shoulder].Rotation);
        given[prop].Rotation = Quaternion.RotationX(0.7f);
        given[prop].Position += new Vector3(0.1f, 0.2f, 0.3f);
        given[prop].Scale = new Vector3(2);
        // A mapped node ignores what it is given
        var upperArm = target[HumanoidBone.LeftUpperArm];
        given[upperArm].Rotation = Quaternion.RotationY(1);
        var result = new TransformTRS[target.Skeleton.Nodes.Length];
        retargeter.Retarget(sourceBones, result, false, out _, given);

        foreach (var node in new[] { shoulder, prop })
        {
            AssertClose(given[node].Rotation, result[node].Rotation, 1e-6f);
            AssertClose(given[node].Position, result[node].Position, 1e-6f);
            AssertClose(given[node].Scale, result[node].Scale, 1e-6f);
        }
        // The mapped bones below the shoulder keep their world rotation
        var restWorld = target.World(atRest);
        var givenWorld = target.World(result);
        foreach (var bone in new[] { HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand })
        {
            restWorld[target[bone]].Decompose(out _, out Quaternion expected, out _);
            givenWorld[target[bone]].Decompose(out _, out Quaternion actual, out _);
            AssertClose(expected, actual, 1e-4f);
        }
    }

    [Fact]
    public void RootMotionAddsUpToTheHipsMotion()
    {
        var source = BuildRig(TPose);
        var target = BuildRig(x => TPose(x) * 2);
        var pose = RandomPose(source, 4, Vector3.Zero);
        var hips = source[HumanoidBone.Hips];
        var turn = Quaternion.RotationY(MathUtil.PiOverTwo);
        pose[hips].Rotation = pose[hips].Rotation * turn;
        pose[hips].Position += new Vector3(0.5f, 0.1f, -1);

        var plain = target.World(Retarget(source, target, pose));
        var split = target.World(Retarget(source, target, pose, true, out var root));

        // The model transform takes the horizontal displacement and the turn; the random tilt of the hips moves their heading a little
        AssertClose(new Vector3(1, 0, -2), root.Position, 1e-4f);
        AssertClose(Vector3.Transform(-Vector3.UnitZ, turn), Vector3.Transform(-Vector3.UnitZ, root.Rotation), 0.2f);
        AssertClose(Vector3.UnitY, Vector3.Transform(Vector3.UnitY, root.Rotation), 1e-4f);

        // A turn alone goes whole to the model transform
        var turned = source.RestLocals;
        turned[hips].Rotation = turn;
        Retarget(source, target, turned, true, out var turnedRoot);
        AssertClose(turn, turnedRoot.Rotation, 1e-4f);

        // The skeleton under the model transform ends where the plain motion does
        var rootMatrix = Matrix.RotationQuaternion(root.Rotation) * Matrix.Translation(root.Position);
        foreach (var bone in Bones)
        {
            var node = target[bone];
            var combined = split[node] * rootMatrix;
            AssertClose(plain[node].TranslationVector, combined.TranslationVector, 1e-4f);
            combined.Decompose(out _, out Quaternion combinedRotation, out _);
            plain[node].Decompose(out _, out Quaternion plainRotation, out _);
            AssertClose(plainRotation, combinedRotation, 1e-4f);
        }
    }

    [Fact]
    public void MissingRequiredBoneIsAnError()
    {
        var source = BuildRig(TPose);
        var target = BuildRig(TPose);
        target.Mapping.RemoveAll(x => x.Bone == HumanoidBone.LeftHand);

        var error = Assert.Throws<InvalidOperationException>(() => new HumanoidRetargeter(source.Skeleton, source.Mapping, target.Skeleton, target.Mapping));
        Assert.Contains("LeftHand", error.Message);
    }

    [Fact]
    public void UnknownNodeIsAnError()
    {
        var source = BuildRig(TPose);
        source.Mapping.Single(x => x.Bone == HumanoidBone.Head).NodeName = "NoSuchNode";

        var error = Assert.Throws<InvalidOperationException>(() => new HumanoidRetargeter(source.Skeleton, source.Mapping, source.Skeleton, BuildRig(TPose).Mapping));
        Assert.Contains("NoSuchNode", error.Message);
    }
}
