// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.Assets.Models.Humanoid;

/// <summary>
/// Finds the standard humanoid bones of a skeleton from the names of its nodes.
/// </summary>
/// <remarks>
/// Each preset is a table of node names, as written by one tool (Mixamo, 3ds Max CAT and Biped, Unreal, SMPL, VRM).
/// Names are compared after removing their namespace and tool prefix, their case, their separators and their side.
/// A bone is kept only if its node is under the node of its parent bone. The preset that maps the most bones wins.
/// </remarks>
public static class HumanoidAutoMapper
{
    private enum Side { None, Left, Right }

    private sealed record Preset(string Name, string[] Prefixes, Dictionary<string, string> Names);

    private static readonly Preset[] Presets =
    [
        new("Mixamo", ["mixamorig"], Table(
            ("hips", "Hips"), ("spine", "Spine"), ("spine1", "Chest"), ("spine2", "UpperChest"), ("neck", "Neck"), ("head", "Head"),
            ("*eye", "Eye"), ("*shoulder", "Shoulder"), ("*arm", "UpperArm"), ("*forearm", "LowerArm"), ("*hand", "Hand"),
            ("*upleg", "UpperLeg"), ("*leg", "LowerLeg"), ("*foot", "Foot"), ("*toebase", "Toes"),
            ("*handthumb1", "ThumbMetacarpal"), ("*handthumb2", "ThumbProximal"), ("*handthumb3", "ThumbDistal"),
            ("*handindex1", "IndexProximal"), ("*handindex2", "IndexIntermediate"), ("*handindex3", "IndexDistal"),
            ("*handmiddle1", "MiddleProximal"), ("*handmiddle2", "MiddleIntermediate"), ("*handmiddle3", "MiddleDistal"),
            ("*handring1", "RingProximal"), ("*handring2", "RingIntermediate"), ("*handring3", "RingDistal"),
            ("*handpinky1", "LittleProximal"), ("*handpinky2", "LittleIntermediate"), ("*handpinky3", "LittleDistal"))),

        new("CAT", ["catrig"], Table(
            ("hub001", "Hips"), ("spine1", "Spine"), ("spine2", "Chest"), ("spine3", "UpperChest"), ("neck1", "Neck"), ("head", "Head"),
            ("*armcollarbone", "Shoulder"), ("*arm1", "UpperArm"), ("*arm2", "LowerArm"), ("*armpalm", "Hand"),
            ("*leg1", "UpperLeg"), ("*leg2", "LowerLeg"), ("*legankle", "Foot"), ("*legdigit11", "Toes"),
            ("*armdigit11", "ThumbMetacarpal"), ("*armdigit12", "ThumbProximal"), ("*armdigit13", "ThumbDistal"),
            ("*armdigit21", "IndexProximal"), ("*armdigit22", "IndexIntermediate"), ("*armdigit23", "IndexDistal"),
            ("*armdigit31", "MiddleProximal"), ("*armdigit32", "MiddleIntermediate"), ("*armdigit33", "MiddleDistal"),
            ("*armdigit41", "RingProximal"), ("*armdigit42", "RingIntermediate"), ("*armdigit43", "RingDistal"),
            ("*armdigit51", "LittleProximal"), ("*armdigit52", "LittleIntermediate"), ("*armdigit53", "LittleDistal"))),

        new("Biped", ["bip"], Table(
            ("pelvis", "Hips"), ("spine", "Spine"), ("spine1", "Chest"), ("spine2", "UpperChest"), ("neck", "Neck"), ("head", "Head"),
            ("*clavicle", "Shoulder"), ("*upperarm", "UpperArm"), ("*forearm", "LowerArm"), ("*hand", "Hand"),
            ("*thigh", "UpperLeg"), ("*calf", "LowerLeg"), ("*foot", "Foot"), ("*toe0", "Toes"),
            ("*finger0", "ThumbMetacarpal"), ("*finger01", "ThumbProximal"), ("*finger02", "ThumbDistal"),
            ("*finger1", "IndexProximal"), ("*finger11", "IndexIntermediate"), ("*finger12", "IndexDistal"),
            ("*finger2", "MiddleProximal"), ("*finger21", "MiddleIntermediate"), ("*finger22", "MiddleDistal"),
            ("*finger3", "RingProximal"), ("*finger31", "RingIntermediate"), ("*finger32", "RingDistal"),
            ("*finger4", "LittleProximal"), ("*finger41", "LittleIntermediate"), ("*finger42", "LittleDistal"))),

        new("Unreal", [], Table(
            ("pelvis", "Hips"), ("spine01", "Spine"), ("spine02", "Chest"), ("spine03", "UpperChest"), ("neck01", "Neck"), ("head", "Head"),
            ("*clavicle", "Shoulder"), ("*upperarm", "UpperArm"), ("*lowerarm", "LowerArm"), ("*hand", "Hand"),
            ("*thigh", "UpperLeg"), ("*calf", "LowerLeg"), ("*foot", "Foot"), ("*ball", "Toes"),
            ("*thumb01", "ThumbMetacarpal"), ("*thumb02", "ThumbProximal"), ("*thumb03", "ThumbDistal"),
            ("*index01", "IndexProximal"), ("*index02", "IndexIntermediate"), ("*index03", "IndexDistal"),
            ("*middle01", "MiddleProximal"), ("*middle02", "MiddleIntermediate"), ("*middle03", "MiddleDistal"),
            ("*ring01", "RingProximal"), ("*ring02", "RingIntermediate"), ("*ring03", "RingDistal"),
            ("*pinky01", "LittleProximal"), ("*pinky02", "LittleIntermediate"), ("*pinky03", "LittleDistal"))),

        new("SMPL", [], Table(
            ("pelvis", "Hips"), ("spine1", "Spine"), ("spine2", "Chest"), ("spine3", "UpperChest"), ("neck", "Neck"), ("head", "Head"), ("jaw", "Jaw"),
            ("*eyesmplhf", "Eye"), ("*collar", "Shoulder"), ("*shoulder", "UpperArm"), ("*elbow", "LowerArm"), ("*wrist", "Hand"),
            ("*hip", "UpperLeg"), ("*knee", "LowerLeg"), ("*ankle", "Foot"), ("*foot", "Toes"),
            ("*thumb1", "ThumbMetacarpal"), ("*thumb2", "ThumbProximal"), ("*thumb3", "ThumbDistal"),
            ("*index1", "IndexProximal"), ("*index2", "IndexIntermediate"), ("*index3", "IndexDistal"),
            ("*middle1", "MiddleProximal"), ("*middle2", "MiddleIntermediate"), ("*middle3", "MiddleDistal"),
            ("*ring1", "RingProximal"), ("*ring2", "RingIntermediate"), ("*ring3", "RingDistal"),
            ("*pinky1", "LittleProximal"), ("*pinky2", "LittleIntermediate"), ("*pinky3", "LittleDistal"))),

        new("VRM", [], VrmTable()),
    ];

    /// <summary>
    /// Finds the humanoid bones of a skeleton.
    /// </summary>
    /// <param name="nodes">The nodes of the skeleton, parents first, as in <see cref="SkeletonAsset.Nodes"/>.</param>
    /// <param name="preset">The name of the preset that matched best, or <c>null</c> when no bone was found.</param>
    /// <returns>The node name found for each bone.</returns>
    internal static Dictionary<HumanoidBone, string> Map(IReadOnlyList<NodeInformation> nodes, out string preset)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        var parents = GetParentIndices(nodes);

        preset = null;
        var best = new Dictionary<HumanoidBone, string>();
        foreach (var candidate in Presets)
        {
            var mapping = MapWithPreset(candidate, nodes, parents);
            if (mapping.Count > best.Count)
            {
                best = mapping;
                preset = candidate.Name;
            }
        }
        return best;
    }

    /// <summary>
    /// Finds the humanoid bones of a skeleton and adds the ones it does not map yet.
    /// </summary>
    /// <param name="skeleton">The skeleton asset. Its existing entries are kept as they are.</param>
    /// <returns>The entries added to <see cref="SkeletonAsset.HumanoidBones"/>.</returns>
    /// <remarks>The nodes of the added entries are set to preserved, so they stay in the compiled skeleton and can be animated.</remarks>
    internal static List<HumanoidBoneMapping> Apply(SkeletonAsset skeleton)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        var added = GetMissingMappings(skeleton.Nodes, skeleton.HumanoidBones);
        foreach (var mapping in added)
        {
            skeleton.HumanoidBones.Add(mapping);
            foreach (var node in skeleton.Nodes.Where(x => x.Name == mapping.NodeName))
                node.Preserve = true;
        }
        return added;
    }

    /// <summary>
    /// Finds the humanoid bones of a skeleton that an existing mapping does not have yet.
    /// </summary>
    /// <param name="nodes">The nodes of the skeleton, parents first.</param>
    /// <param name="existing">The existing mapping.</param>
    /// <returns>The new entries, in the order of <see cref="HumanoidBone"/>, without bones or nodes the existing mapping already uses.</returns>
    public static List<HumanoidBoneMapping> GetMissingMappings(IReadOnlyList<NodeInformation> nodes, IEnumerable<HumanoidBoneMapping> existing)
    {
        ArgumentNullException.ThrowIfNull(existing);
        var usedBones = existing.Where(x => !string.IsNullOrEmpty(x.NodeName)).Select(x => x.Bone).ToHashSet();
        var usedNodes = existing.Where(x => !string.IsNullOrEmpty(x.NodeName)).Select(x => x.NodeName).ToHashSet();
        return Map(nodes, out _)
            .Where(x => !usedBones.Contains(x.Key) && !usedNodes.Contains(x.Value))
            .OrderBy(x => x.Key)
            .Select(x => new HumanoidBoneMapping { Bone = x.Key, NodeName = x.Value })
            .ToList();
    }

    private static Dictionary<HumanoidBone, string> MapWithPreset(Preset preset, IReadOnlyList<NodeInformation> nodes, int[] parents)
    {
        var boneToNode = new Dictionary<HumanoidBone, int>();
        for (var i = 0; i < nodes.Count; ++i)
        {
            var (side, key) = Normalize(nodes[i].Name, preset.Prefixes);
            if (!preset.Names.TryGetValue(side == Side.None ? key : "*" + key, out var stem))
                continue;
            var boneName = side switch { Side.Left => "Left" + stem, Side.Right => "Right" + stem, _ => stem };
            if (Enum.TryParse<HumanoidBone>(boneName, out var bone))
                boneToNode.TryAdd(bone, i);
        }

        // Keep a bone only if its node is under the node of its nearest mapped parent bone (a LowerArm under its UpperArm)
        var result = new Dictionary<HumanoidBone, string>();
        foreach (var (bone, node) in boneToNode.OrderBy(x => x.Key))
        {
            var parentBone = HumanoidBones.GetParent(bone);
            while (parentBone is { } p && !boneToNode.ContainsKey(p))
                parentBone = HumanoidBones.GetParent(p);
            if (parentBone is { } mappedParent && (!result.ContainsKey(mappedParent) || !IsDescendant(parents, node, boneToNode[mappedParent])))
                continue;
            result.Add(bone, nodes[node].Name);
        }
        return result;
    }

    private static bool IsDescendant(int[] parents, int node, int ancestor)
    {
        for (var i = parents[node]; i != -1; i = parents[i])
        {
            if (i == ancestor)
                return true;
        }
        return false;
    }

    private static int[] GetParentIndices(IReadOnlyList<NodeInformation> nodes)
    {
        // Nodes are listed depth first: the parent of a node is the last node before it that is one level up
        var parents = new int[nodes.Count];
        var lastAtDepth = new List<int>();
        for (var i = 0; i < nodes.Count; ++i)
        {
            var depth = Math.Max(0, nodes[i].Depth);
            parents[i] = depth > 0 && depth <= lastAtDepth.Count ? lastAtDepth[depth - 1] : -1;
            if (lastAtDepth.Count > depth)
                lastAtDepth.RemoveRange(depth, lastAtDepth.Count - depth);
            while (lastAtDepth.Count < depth)
                lastAtDepth.Add(-1);
            lastAtDepth.Add(i);
        }
        return parents;
    }

    /// <summary>
    /// Splits a node name into its side and a lower-case key without namespace, tool prefix, side or separators.
    /// </summary>
    private static (Side Side, string Key) Normalize(string name, string[] prefixes)
    {
        if (string.IsNullOrEmpty(name))
            return (Side.None, string.Empty);

        // Namespace, such as "mixamorig:" (the importer already drops it, an asset written by hand may not)
        var namespaceEnd = name.LastIndexOfAny([':', '|']);
        if (namespaceEnd >= 0)
            name = name[(namespaceEnd + 1)..];

        // Tool prefix, such as "mixamorig1_", "CATRig" or "Bip01 ", with the digits and separators after it
        var prefix = Array.Find(prefixes, x => name.StartsWith(x, StringComparison.OrdinalIgnoreCase));
        if (prefix != null)
        {
            var end = prefix.Length;
            while (end < name.Length && (char.IsDigit(name[end]) || IsSeparator(name[end])))
                ++end;
            name = name[end..];
        }

        // Side: "Left"/"Right" first ("LeftForeArm", "left_elbow"), "L_"/"_l" ("lowerarm_l"), or "L" before a capital ("LForearm" of Biped and CAT)
        var side = Side.None;
        if (name.Length > 4 && name.StartsWith("left", StringComparison.OrdinalIgnoreCase))
            (side, name) = (Side.Left, name[4..]);
        else if (name.Length > 5 && name.StartsWith("right", StringComparison.OrdinalIgnoreCase))
            (side, name) = (Side.Right, name[5..]);
        else if (name.Length > 2 && IsSideLetter(name[0]) && IsSeparator(name[1]))
            (side, name) = (SideOf(name[0]), name[2..]);
        else if (name.Length > 2 && IsSeparator(name[^2]) && IsSideLetter(name[^1]))
            (side, name) = (SideOf(name[^1]), name[..^2]);
        else if (name.Length > 2 && name[0] is 'L' or 'R' && char.IsUpper(name[1]))
            (side, name) = (SideOf(name[0]), name[1..]);

        var key = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (!IsSeparator(c))
                key.Append(char.ToLowerInvariant(c));
        }
        return (side, key.ToString());
    }

    private static bool IsSeparator(char c) => c is '_' or '.' or '-' or ' ';

    private static bool IsSideLetter(char c) => c is 'L' or 'l' or 'R' or 'r';

    private static Side SideOf(char c) => c is 'L' or 'l' ? Side.Left : Side.Right;

    private static Dictionary<string, string> Table(params (string Key, string Stem)[] entries)
        => entries.ToDictionary(x => x.Key, x => x.Stem);

    private static Dictionary<string, string> VrmTable()
    {
        // VRM names are the bone names in camel case: "hips", "leftUpperArm"
        var table = new Dictionary<string, string>();
        foreach (var bone in Enum.GetValues<HumanoidBone>())
        {
            var name = bone.ToString();
            var side = HumanoidBones.IsLeft(bone) ? "Left" : HumanoidBones.IsRight(bone) ? "Right" : string.Empty;
            var stem = name[side.Length..];
            table.TryAdd((side.Length > 0 ? "*" : string.Empty) + stem.ToLowerInvariant(), stem);
        }
        return table;
    }
}
