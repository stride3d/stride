// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Rendering;

/// <summary>
/// The morph targets of a <see cref="Mesh"/>: sparse position, normal and tangent deltas, grouped by vertex.
/// </summary>
/// <remarks>
/// The model importer builds this; most code only reads <see cref="TargetNames"/>, and drives weights through
/// <c>ModelComponent</c>. It is shared by every instance of the mesh and holds no weights.
/// Only vertices a target moves have an entry for it, sorted by vertex then target, and <see cref="VertexOffsets"/>
/// gives each vertex's range of entries, so a vertex shader or compute shader reads only the entries of its vertex.
/// Build it with <see cref="Create"/>, which sorts, packs and validates the entries; treat an instance as immutable.
/// </remarks>
[DataContract]
public sealed class MeshMorphData
{
    /// <summary>The maximum number of targets of a mesh: target indices are stored in 16 bits.</summary>
    public const int MaxTargetCount = ushort.MaxValue + 1;

    /// <summary>The number of vertices of the mesh the targets apply to.</summary>
    public int VertexCount;

    /// <summary>The target names, unique within the mesh; a name's index is the target index of its entries.</summary>
    public string[] TargetNames;

    /// <summary>Whether entries may hold normal deltas; when false, the normals are not morphed.</summary>
    public bool HasNormalDeltas;

    /// <summary>Whether entries may hold tangent deltas; when false, the tangents are not morphed.</summary>
    public bool HasTangentDeltas;

    /// <summary>The nonzero deltas, sorted by vertex then target, at most one per vertex and target.</summary>
    public MeshMorphEntry[] Entries;

    /// <summary>
    /// The first entry of each vertex in <see cref="Entries"/>, plus an end value: the entries of vertex <c>v</c> are
    /// <c>VertexOffsets[v]</c> to <c>VertexOffsets[v + 1] - 1</c>. Its length is <see cref="VertexCount"/> + 1.
    /// </summary>
    public uint[] VertexOffsets;

    /// <summary>Creates morph data from the deltas of each vertex and target, in any order.</summary>
    /// <param name="vertexCount">The number of vertices of the mesh.</param>
    /// <param name="names">The target names, nonempty and unique; the names are copied.</param>
    /// <param name="contributions">The deltas; zero entries are dropped.</param>
    /// <param name="hasNormals">Whether the deltas include normals.</param>
    /// <param name="hasTangents">Whether the deltas include tangents.</param>
    /// <returns>The validated morph data.</returns>
    /// <exception cref="ArgumentException">A count or an entry index is out of range.</exception>
    /// <exception cref="InvalidOperationException">
    /// A name is empty or repeated, an entry repeats a vertex and target, a delta is not finite, or a normal or tangent
    /// delta is given without the matching flag.
    /// </exception>
    public static MeshMorphData Create(int vertexCount, ReadOnlySpan<string> names, IEnumerable<MeshMorphEntry> contributions,
        bool hasNormals = false, bool hasTangents = false)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        if (vertexCount < 0 || vertexCount == int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(vertexCount));
        if (names.Length == 0 || names.Length > MaxTargetCount)
            throw new ArgumentException($"Morph target count must be between 1 and {MaxTargetCount}.");
        // Target names are checked by Validate below.
        var entries = new List<MeshMorphEntry>();
        foreach (var entry in contributions)
        {
            if (entry.VertexIndex >= vertexCount || entry.ShapeIndex >= names.Length)
                throw new ArgumentException("Morph contribution index is out of range.");
            CheckFinite(entry);
            if (!entry.IsZero)
                entries.Add(entry);
        }
        entries.Sort((a, b) => a.VertexIndex != b.VertexIndex ? a.VertexIndex.CompareTo(b.VertexIndex) : a.ShapeIndex.CompareTo(b.ShapeIndex));
        var result = new MeshMorphData
        {
            VertexCount = vertexCount,
            TargetNames = names.ToArray(),
            Entries = entries.ToArray(),
            HasNormalDeltas = hasNormals,
            HasTangentDeltas = hasTangents,
            VertexOffsets = new uint[vertexCount + 1],
        };
        foreach (var entry in result.Entries)
        {
            result.VertexOffsets[entry.VertexIndex + 1]++;
        }
        Prefix(result.VertexOffsets);
        result.Validate();
        return result;
    }

    /// <summary>Checks that the data is consistent, for example after loading it, before it is read or uploaded.</summary>
    /// <exception cref="InvalidOperationException">The data is malformed.</exception>
    public void Validate()
    {
        if (VertexCount < 0 || VertexCount == int.MaxValue || TargetNames == null || TargetNames.Length == 0 || TargetNames.Length > MaxTargetCount || Entries == null)
            throw new InvalidOperationException("Invalid morph data header.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in TargetNames)
            if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
                throw new InvalidOperationException("Morph target names must be nonempty and unique.");
        CheckOffsets(VertexOffsets, VertexCount + 1, Entries.Length);
        for (int vertex = 0; vertex < VertexCount; vertex++)
        {
            int previous = -1;
            for (uint i = VertexOffsets[vertex]; i < VertexOffsets[vertex + 1]; i++)
            {
                var entry = Entries[i];
                if (entry.VertexIndex != vertex || entry.ShapeIndex >= TargetNames.Length || entry.ShapeIndex <= previous || entry.IsZero)
                    throw new InvalidOperationException("Invalid or duplicate morph gather contribution.");
                if ((!HasNormalDeltas && entry.HasNormal) || (!HasTangentDeltas && entry.HasTangent))
                    throw new InvalidOperationException("Morph channel flags disagree with the records.");
                CheckFinite(entry);
                previous = entry.ShapeIndex;
            }
        }
    }

    /// <summary>Creates the morph data of a mesh whose vertices were reordered, split or duplicated.</summary>
    /// <param name="sourceVertices">For each vertex of the new mesh, the vertex of this mesh it comes from.</param>
    /// <returns>Morph data with one vertex per element of <paramref name="sourceVertices"/>, each with the deltas of its source.</returns>
    /// <exception cref="ArgumentException">A source vertex is out of range.</exception>
    public MeshMorphData Remap(ReadOnlySpan<uint> sourceVertices)
    {
        Validate();
        var entries = new List<MeshMorphEntry>();
        for (int vertex = 0; vertex < sourceVertices.Length; vertex++)
        {
            uint source = sourceVertices[vertex];
            if (source >= VertexCount)
                throw new ArgumentException("Morph vertex remap is out of range.");
            uint start = VertexOffsets[source];
            uint end = VertexOffsets[source + 1];
            for (uint i = start; i < end; i++)
            {
                var entry = Entries[i];
                entry.VertexIndex = (uint)vertex;
                entries.Add(entry);
            }
        }
        return Create(sourceVertices.Length, TargetNames, entries, HasNormalDeltas, HasTangentDeltas);
    }

    /// <summary>Creates the morph data of the mesh after a transform of its vertices, such as an import scale or axis change.</summary>
    /// <param name="transform">The transform applied to the mesh vertices.</param>
    /// <param name="baseNormals">The mesh normals before the transform; required when <see cref="HasNormalDeltas"/>.</param>
    /// <param name="baseTangents">The mesh tangents before the transform; required when <see cref="HasTangentDeltas"/>.</param>
    /// <returns>The transformed morph data.</returns>
    /// <remarks>
    /// Call it with the attributes before the mesh is transformed. Position deltas are transformed as vectors; normal and
    /// tangent deltas are rebuilt as the difference of the transformed target and base directions.
    /// </remarks>
    /// <exception cref="ArgumentException">A required base attribute is missing or has the wrong length.</exception>
    public MeshMorphData Transform(Matrix transform, ReadOnlySpan<Vector3> baseNormals, ReadOnlySpan<Vector3> baseTangents)
    {
        Validate();
        if ((HasNormalDeltas && baseNormals.Length != VertexCount) || (HasTangentDeltas && baseTangents.Length != VertexCount))
            throw new ArgumentException("Morph transforms require matching base direction attributes.");
        Matrix.Invert(ref transform, out var inverse);
        Matrix.Transpose(ref inverse, out var normalMatrix);
        var entries = new MeshMorphEntry[Entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = Entries[i];
            var position = Vector3.TransformNormal(entry.PositionDelta, transform);
            var normal = HasNormalDeltas ? TransformDirection(baseNormals[(int)entry.VertexIndex], entry.NormalDelta, normalMatrix) : Vector3.Zero;
            var tangent = HasTangentDeltas ? TransformDirection(baseTangents[(int)entry.VertexIndex], entry.TangentDelta, normalMatrix) : Vector3.Zero;
            entries[i] = MeshMorphEntry.Create(entry.VertexIndex, entry.ShapeIndex, position, normal, tangent);
        }
        return Create(VertexCount, TargetNames, entries, HasNormalDeltas, HasTangentDeltas);
    }

    private static Vector3 TransformDirection(Vector3 basis, Vector3 delta, Matrix matrix)
    {
        return Vector3.Normalize(Vector3.TransformNormal(basis + delta, matrix)) - Vector3.Normalize(Vector3.TransformNormal(basis, matrix));
    }

    private static void Prefix(uint[] offsets)
    {
        for (int i = 1; i < offsets.Length; i++)
            offsets[i] = checked(offsets[i] + offsets[i - 1]);
    }

    private static void CheckOffsets(uint[] offsets, int length, int count)
    {
        if (offsets == null || offsets.Length != length || offsets[0] != 0 || offsets[^1] != count)
            throw new InvalidOperationException("Invalid morph offset table.");
        for (int i = 1; i < offsets.Length; i++)
            if (offsets[i] < offsets[i - 1] || offsets[i] > count)
                throw new InvalidOperationException("Morph offsets are not monotonic.");
    }

    private static void CheckFinite(MeshMorphEntry entry)
    {
        if (!entry.IsFinite)
            throw new InvalidOperationException("Non-finite packed morph delta.");
    }
}
