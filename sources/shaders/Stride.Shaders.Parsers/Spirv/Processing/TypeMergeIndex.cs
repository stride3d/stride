// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Processing;

/// <summary>
/// The types, names and decorations of the context that the mixer merges classes into, indexed once for the whole merge.
/// </summary>
/// <remarks>
/// Instructions that other code appends to the context are indexed at the next <see cref="Sync"/>, which the mixer calls at
/// the start of each mixin node. When instructions of the context moved, each call indexes the whole context again first.
/// </remarks>
public sealed class TypeMergeIndex
{
    private readonly SpirvContext context;
    // Hash of a type or import (see Hash) to its positions in the context, in increasing order
    private readonly Dictionary<int, List<int>> types = [];
    // Target ID to the positions of its names and decorations, in the order of CompareDecorations
    private readonly Dictionary<int, List<int>> decorations = [];
    // Positions appended through Add since the last Sync, which are already indexed
    private readonly HashSet<int> added = [];
    private static readonly List<int> NoDecorations = [];
    private int indexedCount;
    private int structureVersion;

    public TypeMergeIndex(SpirvContext context)
    {
        this.context = context;
        Rebuild();
    }

    /// <summary>
    /// Indexes what other code appended to the context since the last call.
    /// </summary>
    public void Sync()
    {
        if (RebuildIfMoved())
            return;

        for (var position = indexedCount; position < context.Count; position++)
        {
            if (!added.Contains(position))
                Index(position);
        }
        added.Clear();
        indexedCount = context.Count;
        VerifyAgainstRebuild();
    }

    /// <summary>
    /// Appends an instruction to the context and indexes it.
    /// </summary>
    public OpDataIndex Add(OpData data)
    {
        RebuildIfMoved();
        var result = context.Add(data);
        Index(result.Index);
        added.Add(result.Index);
        return result;
    }

    /// <summary>
    /// Finds the first type or import of the context with the same operands as <paramref name="data"/>.
    /// </summary>
    public bool CheckForDuplicates(OpData data, out OpDataIndex found)
    {
        RebuildIfMoved();
        if (types.TryGetValue(Hash(data), out var positions))
        {
            foreach (var position in positions)
            {
                if (SameType(context[position].Data, data))
                {
                    found = context[position];
                    return true;
                }
            }
        }
        found = default;
        return false;
    }

    /// <summary>
    /// Turns the names and decorations of a duplicate type into <see cref="Op.OpNop"/>, and returns the decorations that only
    /// one of the two types has, as text. Names may differ.
    /// </summary>
    public List<(string Instruction, bool OnKeepOnly)>? MergeTypeDecorations(int keepId, int removeId)
    {
        RebuildIfMoved();
        var keep = DecorationsOf(keepId);
        var remove = DecorationsOf(removeId);
        List<(string Instruction, bool OnKeepOnly)>? mismatches = null;

        foreach (var position in remove)
        {
            var data = context[position].Data;
            if (data.Op != Op.OpName && !ContainsDecoration(keep, data))
                (mismatches ??= []).Add((data.ToString(), false));
        }
        foreach (var position in keep)
        {
            var data = context[position].Data;
            if (data.Op != Op.OpName && !ContainsDecoration(remove, data))
                (mismatches ??= []).Add((data.ToString(), true));
        }

        foreach (var position in remove)
            context[position].SetOpNop();
        decorations.Remove(removeId);

        return mismatches;
    }

    // The positions are those of the context only while no instruction was inserted before the end, removed, replaced or sorted
    private bool RebuildIfMoved()
    {
        if (context.GetBuffer().StructureVersion == structureVersion)
            return false;
        Rebuild();
        return true;
    }

    private void Rebuild()
    {
        types.Clear();
        decorations.Clear();
        added.Clear();
        for (var position = 0; position < context.Count; position++)
            Index(position);
        indexedCount = context.Count;
        structureVersion = context.GetBuffer().StructureVersion;
    }

    private void Index(int position)
    {
        var data = context[position].Data;
        if (TypeDuplicateHelper.OpCheckDuplicateForTypesAndImport(data.Op))
        {
            var list = ListOf(types, Hash(data));
            var at = list.Count;
            while (at > 0 && list[at - 1] > position)
                at--;
            list.Insert(at, position);
        }
        else if (IsDecoration(data.Op))
        {
            // The target is the first operand of all of them
            var list = ListOf(decorations, data.Memory.Span[1]);
            var at = list.Count;
            while (at > 0 && CompareDecorations(list[at - 1], position) > 0)
                at--;
            list.Insert(at, position);
        }
    }

    // The names and decorations of a target, without the ones turned into OpNop since they were indexed
    private List<int> DecorationsOf(int target)
    {
        if (!decorations.TryGetValue(target, out var list))
            return NoDecorations;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (context[list[i]].Data.Op == Op.OpNop)
                list.RemoveAt(i);
        }
        return list;
    }

    private bool ContainsDecoration(List<int> positions, OpData data)
    {
        foreach (var position in positions)
        {
            var other = context[position].Data;
            if (other.Op == data.Op && other.Memory.Span[2..].SequenceEqual(data.Memory.Span[2..]))
                return true;
        }
        return false;
    }

    // The order of TypeDuplicateHelper's sorted list of names: by op, then operands after the target, then position
    private int CompareDecorations(int position1, int position2)
    {
        var data1 = context[position1].Data;
        var data2 = context[position2].Data;
        var comparison = ((int)data1.Op).CompareTo((int)data2.Op);
        if (comparison == 0)
            comparison = data1.Memory.Span[2..].SequenceCompareTo(data2.Memory.Span[2..]);
        return comparison != 0 ? comparison : position1.CompareTo(position2);
    }

    // Covers what SameType compares: the op and the operands after the result ID, with the value of an array's length
    private int Hash(OpData data)
    {
        var words = data.Memory.Span;
        var hash = new HashCode();
        hash.Add(data.Op);
        if (data.Op == Op.OpTypeArray)
        {
            hash.Add(words[2]);
            hash.Add(ArrayLength(words[3]));
        }
        else
            hash.AddBytes(MemoryMarshal.AsBytes(words[2..]));
        return hash.ToHashCode();
    }

    // As TypeDuplicateHelper compares types: arrays by element type and the value of their length when it is known
    private bool SameType(OpData data1, OpData data2)
    {
        if (data1.Op != data2.Op)
            return false;
        var words1 = data1.Memory.Span;
        var words2 = data2.Memory.Span;
        return data1.Op == Op.OpTypeArray
            ? words1[2] == words2[2] && ArrayLength(words1[3]) == ArrayLength(words2[3])
            : words1[2..].SequenceEqual(words2[2..]);
    }

    private (bool IsValue, int Value) ArrayLength(int constantId)
        => context.TryGetConstantValue(constantId, out var value, out _) ? (true, Convert.ToInt32(value)) : (false, constantId);

    private static bool IsDecoration(Op op)
        => op is Op.OpName or Op.OpMemberName or Op.OpDecorate or Op.OpDecorateString or Op.OpMemberDecorate or Op.OpMemberDecorateString;

    private static List<int> ListOf(Dictionary<int, List<int>> map, int key)
    {
        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);
        return list ??= [];
    }

    // Debug builds, such as the corpus job of CI, check that the index kept current matches one built from the context
    [Conditional("DEBUG")]
    private void VerifyAgainstRebuild()
    {
        var rebuilt = new TypeMergeIndex(context);
        foreach (var (hash, positions) in rebuilt.types)
        {
            if (!types.TryGetValue(hash, out var kept) || !kept.SequenceEqual(positions))
                throw new InvalidOperationException($"The mixer's type index differs from the context at position {positions[0]} ({context[positions[0]].Data})");
        }
        if (types.Count != rebuilt.types.Count)
            throw new InvalidOperationException("The mixer's type index has types that the context doesn't have");

        foreach (var target in decorations.Keys.Union(rebuilt.decorations.Keys).ToList())
        {
            if (!DecorationsOf(target).SequenceEqual(rebuilt.DecorationsOf(target)))
                throw new InvalidOperationException($"The mixer's index of names and decorations differs from the context for target {target}");
        }
    }
}
