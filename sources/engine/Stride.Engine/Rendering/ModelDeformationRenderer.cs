// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering.ComputeEffect;
using Stride.Rendering.Materials;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering;

// Rendering owns shared immutable inputs. Meshes with morph targets deform in compute when the device supports it:
// instances of the same mesh share batches of up to MaxBatchSize output slots, so one dispatch deforms every instance
// in a batch, skinning included. Otherwise each instance only owns its morph weights, and its render meshes apply them
// in TransformationMorph. Skinning-only meshes keep vertex shader skinning unless an override forces compute.
// ModelRenderProcessor schedules every model each frame; MeshRenderFeature then deforms only the models a view
// collected, after every view is culled and before any is drawn.
internal sealed class ModelDeformationRenderer : IModelDeformer, IDisposable
{
    // Direct3D 11 limit on thread groups per dispatch dimension.
    private const int MaxThreadGroupsPerDimension = 65535;
    // Instances of a mesh deformed by one dispatch. Outputs grow on demand, so it costs no memory up front.
    private const int MaxBatchSize = 65535;
    // Matrices are uploaded as their first three float4 columns (a 3x4 matrix).
    private const int RowsPerMatrix = 3;
    // An instance record: flags and pose offset, then the inverse mesh world matrix.
    private const int RowsPerRecord = 1 + RowsPerMatrix;
    // Bits of an instance record's flags, see MorphSkinning.
    private const uint InstanceActive = 1;
    private const uint InstanceSkinned = 2;
    private const uint InstanceMorphed = 4;
    // Per-mesh skinning flags, see MorphSkinning. Normal and tangent skinning require position skinning.
    private const uint SkinPosition = 1;
    private const uint SkinNormal = 2;
    private const uint SkinTangent = 4;
    // Batches with fewer instance slots dispatch X32Y1 thread groups, larger ones X8Y16 (measured on a 600-target mesh).
    private const int InstanceGroupThreshold = 4;
    // Below this the mesh transform cannot be inverted to bring bones into mesh space.
    private const float MinTransformDeterminant = 1e-12f;
    private static readonly int DwordSize = sizeof(uint);
    private static readonly int RowSize = Unsafe.SizeOf<Vector4>();
    private static readonly int Vector3Size = Unsafe.SizeOf<Vector3>();
    // The compute kernel reads entries as dword pairs (StructuredBuffer<uint2>); the vertex shader reads single dwords.
    private static readonly int EntryPairSize = 2 * sizeof(uint);

    private readonly Dictionary<(Mesh, MeshDraw, MeshMorphData), SharedMesh> sharedMeshes = new();
    private readonly Dictionary<ModelComponent, Instance[]> instances = new();
    private readonly Dictionary<DeformationThreadGroup, ComputeEffectShader> effects = new();
    private readonly HashSet<Batch> pending = new();
    private readonly List<Instance> pendingWeights = new();
    // Models scheduled this frame and not deformed yet: a view must collect them first.
    private readonly Dictionary<RenderModel, ModelComponent> scheduled = new();
    private readonly RenderDrawContext context;
    // Per-frame data of every batch, each uploaded once per frame: node world matrices of each skeleton (one pose per
    // skeleton, shared by all its meshes), instance records and morph weights.
    private readonly FrameArena<Vector4> poses = new();
    private readonly FrameArena<Vector4> records = new();
    private readonly FrameArena<float> weights = new();
    private readonly Dictionary<SkeletonUpdater, int> poseOffsets = new();
    private MeshDeformationOverride deformationOverride;

    public ModelDeformationRenderer(RenderDrawContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Compute deformation needs compute shaders on the device and a shader profile with a compute stage:
    /// effects compile for the requested profile, which may be lower than what the device supports.
    /// </summary>
    private bool ComputeAvailable
    {
        get
        {
            var device = context.GraphicsDevice;
            var shaderProfile = device.ShaderProfile ?? device.Features.RequestedProfile;
            return device.Features.HasComputeShaders && shaderProfile >= GraphicsProfile.Level_11_0;
        }
    }

    public void Configure(MeshDeformationOverride value)
    {
        if (value == deformationOverride)
            return;
        if (value?.Mode == MeshDeformationMode.Compute && !ComputeAvailable)
            throw new NotSupportedException("Compute deformation requires compute shader support on this graphics device.");
        foreach (var component in instances.Keys.ToArray())
            Remove(component);
        deformationOverride = value;
    }

    /// <summary>Forgets the models scheduled last frame and not collected by any view.</summary>
    public void BeginFrame() => scheduled.Clear();

    /// <summary>The models scheduled this frame that no view collected, so they were not deformed.</summary>
    internal int UncollectedModelCount => scheduled.Count;

    public void Remove(ModelComponent component)
    {
        if (!instances.Remove(component, out var owned))
            return;
        foreach (var instance in owned)
            if (instance != null)
                Release(instance);
    }

    private void Release(Instance instance)
    {
        var sharedMesh = instance.Shared;
        instance.Dispose();
        pendingWeights.Remove(instance);
        if (instance.Batch?.Used == 0)
        {
            sharedMesh.Batches.Remove(instance.Batch);
            pending.Remove(instance.Batch);
            instance.Batch.Dispose();
        }
        if (--sharedMesh.Users == 0)
        {
            sharedMeshes.Remove((sharedMesh.Source, sharedMesh.Draw, sharedMesh.MorphData));
            sharedMesh.Dispose();
        }
    }

    /// <summary>
    /// Updates the instances of <paramref name="model"/>, points its render meshes at their deformed vertices and keeps
    /// this frame's deformation until a view collects the model.
    /// </summary>
    public void Schedule(ModelComponent model, RenderModel renderModel)
    {
        if (!model.Enabled || model.Model == null)
        {
            Remove(model);
            return;
        }
        bool morphEnabled = model.Morphs.Enabled && model.HasMorphTargets;
        if (morphEnabled)
            model.PrepareMorphWeights();
        var meshes = model.Model.Meshes;
        var owned = OwnedInstances(model, meshes);
        var forced = deformationOverride?.Mode;
        bool morphCompute = forced == MeshDeformationMode.Compute || (forced == null && ComputeAvailable);
        for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
        {
            var mesh = meshes[meshIndex];
            bool deformMorph = morphEnabled && mesh.MorphTargets?.VertexCount > 0;
            bool deformSkin = mesh.Skinning != null && (forced == MeshDeformationMode.Compute || (deformMorph && morphCompute));
            bool compute = deformSkin || (deformMorph && morphCompute);
            bool deformed = deformMorph || deformSkin;
            // Release an instance prepared for the other path, or of a mesh that is no longer deformed.
            if (owned[meshIndex] is { } previous && (previous.Shared.Compute != compute || !deformed))
            {
                Release(previous);
                owned[meshIndex] = null;
            }
            if (!deformed)
                continue;
            var instance = owned[meshIndex] ??= CreateInstance(mesh, compute);
            var materialInfo = renderModel.Materials[meshIndex];
            if (materialInfo.MeshCount == 0)
                continue;
            Matrix world = renderModel.Meshes[materialInfo.MeshStartIndex].World;
            var blendMatrices = model.MeshInfos[meshIndex].BlendMatrices;
            instance.Schedule(deformMorph ? model.GetMorphWeights(meshIndex) : instance.ZeroWeights, deformSkin ? blendMatrices : Array.Empty<Matrix>(), world, deformMorph);
            if (compute)
                instance.Mesh.Skinning = deformSkin ? null : mesh.Skinning;
            for (int pass = 0; pass < materialInfo.MeshCount; pass++)
            {
                var rendered = renderModel.Meshes[materialInfo.MeshStartIndex + pass];
                if (compute)
                {
                    rendered.Mesh = instance.Mesh;
                    rendered.ActiveMeshDraw = instance.Mesh.Draw;
                    rendered.BlendMatrices = deformSkin ? null : blendMatrices;
                }
                else
                    rendered.MorphBuffers = instance.Buffers;
            }
        }
        renderModel.Deformer = this;
        scheduled[renderModel] = model;
    }

    /// <summary>Writes the scheduled deformation of <paramref name="renderModel"/>; <see cref="Flush"/> uploads and dispatches it.</summary>
    public void Deform(RenderModel renderModel)
    {
        if (!scheduled.Remove(renderModel, out var model) || !instances.TryGetValue(model, out var owned))
            return;
        int pose = -1;
        foreach (var instance in owned)
        {
            if (instance is not { IsScheduled: true })
                continue;
            if (instance.IsSkinned && pose < 0)
                pose = Pose(model.Skeleton);
            instance.Prepare(pose);
            if (instance.Batch != null)
                pending.Add(instance.Batch);
            else
                pendingWeights.Add(instance);
        }
    }

    // The instances of a model, recreated when its meshes were replaced or edited.
    private Instance[] OwnedInstances(ModelComponent model, List<Mesh> meshes)
    {
        if (instances.TryGetValue(model, out var owned) && !Matches(owned, meshes))
        {
            Remove(model);
            owned = null;
        }
        if (owned == null)
            instances[model] = owned = new Instance[meshes.Count];
        return owned;
    }

    private static bool Matches(Instance[] owned, List<Mesh> meshes)
    {
        if (owned.Length != meshes.Count)
            return false;
        for (int meshIndex = 0; meshIndex < owned.Length; meshIndex++)
        {
            var sharedMesh = owned[meshIndex]?.Shared;
            if (sharedMesh != null && (sharedMesh.Source != meshes[meshIndex] || sharedMesh.MorphData != meshes[meshIndex].MorphTargets || sharedMesh.Draw != meshes[meshIndex].Draw))
                return false;
        }
        return true;
    }

    private Instance CreateInstance(Mesh mesh, bool compute)
    {
        var key = (mesh, mesh.Draw, mesh.MorphTargets);
        if (!sharedMeshes.TryGetValue(key, out var sharedMesh))
            sharedMeshes[key] = sharedMesh = CreateSharedMesh(mesh, compute);
        try
        {
            var instance = new Instance(context.GraphicsDevice, sharedMesh, compute ? BatchWithFreeSlot(sharedMesh) : null);
            sharedMesh.Users++;
            return instance;
        }
        catch
        {
            if (sharedMesh.Users == 0)
            {
                sharedMeshes.Remove(key);
                sharedMesh.Dispose();
            }
            throw;
        }
    }

    private SharedMesh CreateSharedMesh(Mesh mesh, bool compute)
    {
        // Copies of a mesh share its draw and morph data: clone the prepared twin instead of preparing again.
        int boneCount = mesh.Skinning?.Bones.Length ?? 0;
        var twin = sharedMeshes.Values.FirstOrDefault(candidate => candidate.Draw == mesh.Draw && candidate.MorphData == mesh.MorphTargets && candidate.Compute == compute && candidate.BoneCount == boneCount);
        return twin != null ? new SharedMesh(context, mesh, twin) : new SharedMesh(context, mesh, compute);
    }

    // A batch with a free slot; otherwise grow a batch below the maximum size before starting another one.
    private Batch BatchWithFreeSlot(SharedMesh sharedMesh)
    {
        var batch = sharedMesh.Batches.FirstOrDefault(candidate => candidate.Used < candidate.Capacity);
        if (batch != null)
            return batch;
        batch = sharedMesh.Batches.FirstOrDefault(candidate => candidate.Capacity < MaxBatchSize);
        if (batch != null)
            batch.Grow(context.GraphicsDevice, Math.Min(batch.Capacity * 2, MaxBatchSize));
        else
            sharedMesh.Batches.Add(batch = new Batch(context.GraphicsDevice, sharedMesh, 1));
        return batch;
    }

    // Appends the node world matrices of a skeleton once per frame, for all the meshes skinned by it.
    private int Pose(SkeletonUpdater skeleton)
    {
        if (poseOffsets.TryGetValue(skeleton, out int offset))
            return offset;
        var nodes = skeleton.NodeTransformations;
        offset = poses.Allocate(nodes.Length * RowsPerMatrix);
        var rows = poses.Span(offset, nodes.Length * RowsPerMatrix);
        for (int node = 0; node < nodes.Length; node++)
            WriteColumns(nodes[node].WorldMatrix, rows.Slice(node * RowsPerMatrix));
        poseOffsets.Add(skeleton, offset);
        return offset;
    }

    // Stride uses row vectors (p' = p * M): the first three columns let the shader compute each output component
    // as one dot product with float4(p, 1).
    private static void WriteColumns(in Matrix matrix, Span<Vector4> rows)
    {
        rows[0] = matrix.Column1;
        rows[1] = matrix.Column2;
        rows[2] = matrix.Column3;
    }

    public void Flush()
    {
        var command = context.CommandList;
        foreach (var instance in pendingWeights)
            instance.Upload(command);
        pendingWeights.Clear();
        if (pending.Count == 0)
            return;
        foreach (var batch in pending)
            batch.Write(records, weights);
        poses.Upload(context.GraphicsDevice, command);
        records.Upload(context.GraphicsDevice, command);
        weights.Upload(context.GraphicsDevice, command);
        // Transition every output once before and once after all dispatches, not around each one: the outputs are
        // distinct, so a barrier per dispatch would only stop the GPU from overlapping small dispatches.
        foreach (var batch in pending)
            command.ResourceBarrierTransition(batch.Output, BarrierLayout.UnorderedAccess);
        foreach (var batch in pending)
            Dispatch(batch);
        foreach (var batch in pending)
        {
            command.ResourceBarrierTransition(batch.Output, BarrierLayout.Common);
            batch.EndFrame();
        }
        pending.Clear();
        poseOffsets.Clear();
    }

    private void Dispatch(Batch batch)
    {
        var sharedMesh = batch.Shared;
        var group = deformationOverride?.ThreadGroup
            ?? (batch.SlotCount < InstanceGroupThreshold ? DeformationThreadGroup.X32Y1 : DeformationThreadGroup.X8Y16);
        var (groupX, groupY) = ThreadNumbers(group);
        var effect = Effect(group);
        var parameters = effect.Parameters;
        parameters.Set(Keys.Input, sharedMesh.Input);
        parameters.Set(Keys.Entries, sharedMesh.Entries);
        parameters.Set(Keys.Offsets, sharedMesh.Offsets);
        parameters.Set(Keys.Weights, weights.Buffer);
        parameters.Set(Keys.Poses, poses.Buffer);
        parameters.Set(Keys.Records, records.Buffer);
        parameters.Set(Keys.BoneBinds, sharedMesh.BoneBinds);
        parameters.Set(Keys.BoneNodes, sharedMesh.BoneNodes);
        parameters.Set(Keys.RecordBase, (uint)batch.RecordBase);
        parameters.Set(Keys.WeightBase, (uint)batch.WeightBase);
        parameters.Set(Keys.Output, batch.Output);
        parameters.Set(Keys.VertexCount, (uint)sharedMesh.Binding.Count);
        parameters.Set(Keys.Stride, (uint)sharedMesh.Binding.Stride);
        parameters.Set(Keys.Position, (uint)sharedMesh.PositionOffset);
        parameters.Set(Keys.Normal, sharedMesh.NormalOffset);
        parameters.Set(Keys.Tangent, sharedMesh.TangentOffset);
        parameters.Set(Keys.Indices, sharedMesh.IndicesOffset);
        parameters.Set(Keys.BlendWeights, sharedMesh.WeightsOffset);
        parameters.Set(Keys.BoneCount, (uint)sharedMesh.BoneCount);
        var meshParameters = sharedMesh.Source.Parameters;
        uint skinningFlags = meshParameters.Get(MaterialKeys.HasSkinningPosition)
            ? SkinPosition | (meshParameters.Get(MaterialKeys.HasSkinningNormal) ? SkinNormal : 0)
                           | (meshParameters.Get(MaterialKeys.HasSkinningTangent) ? SkinTangent : 0)
            : 0;
        parameters.Set(Keys.SkinningFlags, skinningFlags);
        parameters.Set(Keys.MorphFlags, (uint)sharedMesh.MorphFlags);
        parameters.Set(Keys.Index16, (uint)sharedMesh.Index16);
        parameters.Set(Keys.TargetCount, (uint)sharedMesh.TargetCount);
        parameters.Set(Keys.ActiveCount, (uint)batch.ActiveCount);
        parameters.Set(Keys.InstanceCount, (uint)batch.SlotCount);
        parameters.Set(Keys.OutputStride, (uint)batch.SlotBytes);
        parameters.Set(Keys.OutputVertexStride, (uint)sharedMesh.OutputVertexStride);
        parameters.Set(Keys.OutputNormal, sharedMesh.OutputNormalOffset);
        parameters.Set(Keys.OutputTangent, sharedMesh.OutputTangentOffset);
        parameters.Set(Keys.TangentW, (uint)sharedMesh.TangentW);
        int vertexGroups = (sharedMesh.Binding.Count + groupX - 1) / groupX;
        if (vertexGroups > MaxThreadGroupsPerDimension)
            throw new NotSupportedException($"Compute deformation supports at most {MaxThreadGroupsPerDimension} vertex thread groups per mesh.");
        int instanceGroups = (batch.SlotCount + groupY - 1) / groupY;
        if (instanceGroups > MaxThreadGroupsPerDimension)
            throw new NotSupportedException($"Compute deformation supports at most {MaxThreadGroupsPerDimension} instance thread groups per dispatch.");
        effect.ThreadGroupCounts = new Int3(vertexGroups, instanceGroups, 1);
        effect.Draw(context);
    }

    private ComputeEffectShader Effect(DeformationThreadGroup group)
    {
        if (effects.TryGetValue(group, out var effect))
            return effect;
        var (threadsX, threadsY) = ThreadNumbers(group);
        effect = new ComputeEffectShader(context.RenderContext) { ShaderSourceName = "MorphSkinning", ThreadNumbers = new Int3(threadsX, threadsY, 1) };
        effects.Add(group, effect);
        return effect;
    }

    private static (int X, int Y) ThreadNumbers(DeformationThreadGroup group)
    {
        return group switch
        {
            DeformationThreadGroup.X32Y1 => (32, 1),
            DeformationThreadGroup.X8Y16 => (8, 16),
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null),
        };
    }

    public void Dispose()
    {
        foreach (var component in instances.Keys.ToArray())
            Remove(component);
        foreach (var effect in effects.Values)
            effect.Dispose();
        effects.Clear();
        poses.Dispose();
        records.Dispose();
        weights.Dispose();
        context.Dispose();
    }

    private sealed class SharedMesh : IDisposable
    {
        public readonly Mesh Source;
        public readonly MeshDraw Draw;
        public readonly MeshMorphData MorphData;
        public readonly Buffer Input;
        public Buffer Entries;
        public Buffer Offsets;
        // Per mesh bone: the inverse bind matrix (three float4 columns) and the skeleton node it follows.
        public Buffer BoneBinds;
        public Buffer BoneNodes;
        public readonly int PositionOffset;
        public readonly int NormalOffset;
        public readonly int TangentOffset;
        public readonly int IndicesOffset;
        public readonly int WeightsOffset;
        public readonly int Index16;
        public readonly int MorphFlags;
        public readonly int TargetCount;
        public readonly int BoneCount;
        public readonly bool Compute;
        public readonly byte[] VertexBytes;
        public readonly VertexBufferBinding Binding;
        // Compute mode draws two streams: the source vertices without the deformed attributes, shared by all instances,
        // and a compact per-instance output with position, normal and tangent.
        public readonly VertexBufferBinding SourceStream;
        public readonly VertexDeclaration OutputDeclaration;
        public readonly int OutputVertexStride;
        public readonly int OutputNormalOffset = -1;
        public readonly int OutputTangentOffset = -1;
        public readonly int TangentW;
        public readonly List<Batch> Batches = new();
        public int Users;

        /// <summary>
        /// Prepares a mesh with the same draw and morph data as <paramref name="twin"/>: the read-only CPU arrays are shared
        /// and the GPU buffers are copied on the GPU, so the mesh still owns its buffers like a distinct asset.
        /// </summary>
        public SharedMesh(RenderDrawContext context, Mesh mesh, SharedMesh twin)
        {
            Source = mesh;
            (Draw, MorphData, Compute, Binding, VertexBytes) = (twin.Draw, twin.MorphData, twin.Compute, twin.Binding, twin.VertexBytes);
            (SourceStream, OutputDeclaration, OutputVertexStride, OutputNormalOffset, OutputTangentOffset, TangentW) =
                (twin.SourceStream, twin.OutputDeclaration, twin.OutputVertexStride, twin.OutputNormalOffset, twin.OutputTangentOffset, twin.TangentW);
            (TargetCount, BoneCount) = (twin.TargetCount, twin.BoneCount);
            (PositionOffset, NormalOffset, TangentOffset, IndicesOffset, WeightsOffset, Index16, MorphFlags) =
                (twin.PositionOffset, twin.NormalOffset, twin.TangentOffset, twin.IndicesOffset, twin.WeightsOffset, twin.Index16, twin.MorphFlags);
            Buffer Clone(Buffer source)
            {
                if (source == null)
                    return null;
                var copy = Buffer.New(context.GraphicsDevice, source.SizeInBytes, source.StructureByteStride, source.Flags, source.ViewFormat);
                context.CommandList.Copy(source, copy);
                return copy;
            }
            try
            {
                (Input, Entries, Offsets) = (Clone(twin.Input), Clone(twin.Entries), Clone(twin.Offsets));
                CreateBoneTables(context.GraphicsDevice, mesh);
            }
            catch { Dispose(); throw; }
        }

        public SharedMesh(RenderDrawContext context, Mesh mesh, bool compute)
        {
            var device = context.GraphicsDevice;
            Compute = compute;
            Source = mesh;
            Draw = mesh.Draw;
            MorphData = mesh.MorphTargets;
            MorphData?.Validate();
            TargetCount = MorphData?.TargetNames.Length ?? 0;
            BoneCount = mesh.Skinning?.Bones.Length ?? 0;
            // Both paths index the morph offsets by vertex, so they must cover every vertex.
            if (MorphData != null && Draw.VertexBuffers[0].Count != MorphData.VertexCount)
                throw new InvalidOperationException("Morph target vertex count does not match the mesh vertex count.");
            if (!compute)
            {
                // The vertex shader reads the mesh vertices; only morph data is uploaded.
                var declarations = Draw.VertexBuffers.Select(vertexBuffer => vertexBuffer.Declaration).ToArray();
                bool HasElement(string name) => declarations.Any(declaration => declaration.VertexElements.Any(element => element.SemanticName == name && element.SemanticIndex == 0));
                MorphFlags = MeshMorphBuffers.Enabled;
                if (HasElement("NORMAL"))
                    MorphFlags |= MeshMorphBuffers.Normal | (HasElement("TANGENT") ? MeshMorphBuffers.Tangent : 0);
                try { CreateMorphBuffers(context.GraphicsDevice, Draw.VertexBuffers[0].Count, DwordSize); }
                catch { Dispose(); throw; }
                return;
            }
            if (Draw.VertexBuffers.Length != 1)
                throw new NotSupportedException("Compute deformation requires an interleaved vertex buffer.");
            Binding = Draw.VertexBuffers[0];
            if (Binding.Stride % DwordSize != 0)
                throw new InvalidOperationException("Deformation vertex layout mismatch.");
            var elements = Binding.Declaration.EnumerateWithOffsets().ToArray();
            int Offset(string name, bool required, params PixelFormat[] formats)
            {
                var matches = elements.Where(placedElement => placedElement.VertexElement.SemanticName == name && placedElement.VertexElement.SemanticIndex == 0).ToArray();
                if (matches.Length == 0 && !required)
                    return -1;
                if (matches.Length != 1 || matches[0].Offset % DwordSize != 0 || !formats.Contains(matches[0].VertexElement.Format))
                    throw new NotSupportedException($"Unsupported compute deformation vertex attribute: {name}.");
                return matches[0].Offset;
            }
            PositionOffset = Offset("POSITION", true, PixelFormat.R32G32B32_Float);
            NormalOffset = Offset("NORMAL", false, PixelFormat.R32G32B32_Float);
            TangentOffset = Offset("TANGENT", false, PixelFormat.R32G32B32_Float, PixelFormat.R32G32B32A32_Float);
            MorphFlags = MeshMorphBuffers.Enabled;
            if (NormalOffset >= 0)
                MorphFlags |= MeshMorphBuffers.Normal | (TangentOffset >= 0 ? MeshMorphBuffers.Tangent : 0);
            IndicesOffset = Offset("BLENDINDICES", mesh.Skinning != null, PixelFormat.R8G8B8A8_UInt, PixelFormat.R16G16B16A16_UInt);
            WeightsOffset = Offset("BLENDWEIGHT", mesh.Skinning != null, PixelFormat.R32G32B32A32_Float);
            Index16 = elements.Any(placedElement => placedElement.VertexElement.SemanticName == "BLENDINDICES" && placedElement.VertexElement.Format == PixelFormat.R16G16B16A16_UInt) ? 1 : 0;
            bool IsDeformed(VertexElement element) => element.SemanticIndex == 0 && element.SemanticName is "POSITION" or "NORMAL" or "TANGENT";
            var source = elements.Where(placedElement => !IsDeformed(placedElement.VertexElement))
                .Select(placedElement => new VertexElement(placedElement.VertexElement.SemanticName, placedElement.VertexElement.SemanticIndex, placedElement.VertexElement.Format, placedElement.Offset)).ToArray();
            SourceStream = new VertexBufferBinding(Binding.Buffer, new VertexDeclaration(source, 0, Binding.Stride), Binding.Count, Binding.Stride, Binding.Offset);
            var output = new List<VertexElement> { VertexElement.Position<Vector3>(0, 0) };
            OutputVertexStride = Vector3Size;
            if (NormalOffset >= 0)
            {
                OutputNormalOffset = OutputVertexStride;
                output.Add(VertexElement.Normal<Vector3>(0, OutputVertexStride));
                OutputVertexStride += Vector3Size;
            }
            if (TangentOffset >= 0)
            {
                var format = elements.First(placedElement => placedElement.VertexElement.SemanticName == "TANGENT" && placedElement.VertexElement.SemanticIndex == 0).VertexElement.Format;
                TangentW = format == PixelFormat.R32G32B32A32_Float ? 1 : 0;
                OutputTangentOffset = OutputVertexStride;
                output.Add(new VertexElement("TANGENT", 0, format, OutputVertexStride));
                OutputVertexStride += format.SizeInBytes;
            }
            OutputDeclaration = new VertexDeclaration(output.ToArray(), 0, OutputVertexStride);
            // Runtime content loading discards serialized vertex bytes. Read them
            // back once when preparing this shared mesh, never during normal updates.
            var bytes = Binding.Buffer.GetSerializationData()?.Content ?? Binding.Buffer.GetData<byte>(context.CommandList);
            VertexBytes = bytes.AsSpan(Binding.Offset, checked(Binding.Count * Binding.Stride)).ToArray();
            try
            {
                Input = Buffer.Raw.New(device, VertexBytes.AsSpan(), BufferFlags.ShaderResource);
                CreateMorphBuffers(device, Binding.Count, EntryPairSize);
                CreateBoneTables(device, mesh);
            }
            catch { Dispose(); throw; }
        }

        private void CreateMorphBuffers(GraphicsDevice device, int vertexCount, int entryStride)
        {
            Buffer Structured(ReadOnlySpan<byte> value, int stride) => Buffer.New(device, value.Length == 0 ? new byte[stride] : value, stride, BufferFlags.StructuredBuffer | BufferFlags.ShaderResource);
            Entries = Structured(MemoryMarshal.AsBytes((MorphData?.Entries ?? Array.Empty<MeshMorphEntry>()).AsSpan()), entryStride);
            Offsets = Structured(MemoryMarshal.AsBytes((MorphData?.VertexOffsets ?? new uint[vertexCount + 1]).AsSpan()), DwordSize);
        }

        private void CreateBoneTables(GraphicsDevice device, Mesh mesh)
        {
            var bones = mesh.Skinning?.Bones ?? Array.Empty<MeshBoneDefinition>();
            var binds = new Vector4[Math.Max(bones.Length, 1) * RowsPerMatrix];
            var nodes = new uint[Math.Max(bones.Length, 1)];
            for (int bone = 0; bone < bones.Length; bone++)
            {
                WriteColumns(bones[bone].LinkToMeshMatrix, binds.AsSpan(bone * RowsPerMatrix));
                nodes[bone] = (uint)bones[bone].NodeIndex;
            }
            BoneBinds = Buffer.New(device, MemoryMarshal.AsBytes(binds.AsSpan()), RowSize, BufferFlags.StructuredBuffer | BufferFlags.ShaderResource);
            BoneNodes = Buffer.New(device, MemoryMarshal.AsBytes(nodes.AsSpan()), DwordSize, BufferFlags.StructuredBuffer | BufferFlags.ShaderResource);
        }

        public void Dispose()
        {
            Input?.Dispose();
            Entries?.Dispose();
            Offsets?.Dispose();
            BoneBinds?.Dispose();
            BoneNodes?.Dispose();
        }
    }

    // Output slots for instances of one shared mesh, deformed by one dispatch. A batch starts with one slot and doubles,
    // up to the batch size, as instances arrive, so meshes with few instances do not hold memory for a full batch.
    private sealed class Batch : IDisposable
    {
        public readonly SharedMesh Shared;
        public readonly int SlotBytes;
        public int Capacity;
        public Buffer Output;
        // Per slot: morph weights, flags, pose offset and inverse mesh world columns, copied into the frame arenas.
        private float[] weightData;
        private uint[] flagData;
        private uint[] poseData;
        private Vector4[] inverseWorlds;
        private bool[] occupied;
        // Instance meshes drawing from Output, rebound when it is reallocated.
        private readonly Dictionary<int, Mesh> bound = new();
        public int Used;
        public int SlotCount;
        public int ActiveCount;
        public int RecordBase;
        public int WeightBase;

        public Batch(GraphicsDevice device, SharedMesh shared, int capacity)
        {
            Shared = shared;
            SlotBytes = checked(shared.Binding.Count * shared.OutputVertexStride);
            occupied = Array.Empty<bool>();
            weightData = Array.Empty<float>();
            flagData = Array.Empty<uint>();
            poseData = Array.Empty<uint>();
            inverseWorlds = Array.Empty<Vector4>();
            Grow(device, capacity);
        }

        /// <summary>Reallocates the output for <paramref name="capacity"/> slots. Active slots are deformed again in the same frame.</summary>
        public void Grow(GraphicsDevice device, int capacity)
        {
            Array.Resize(ref occupied, capacity);
            Array.Resize(ref weightData, capacity * Shared.TargetCount);
            Array.Resize(ref flagData, capacity);
            Array.Resize(ref poseData, capacity);
            Array.Resize(ref inverseWorlds, capacity * RowsPerMatrix);
            Capacity = capacity;
            Dispose();
            Output = Buffer.Raw.New(device, checked(capacity * SlotBytes), BufferFlags.VertexBuffer | BufferFlags.ShaderResource | BufferFlags.UnorderedAccess);
            foreach (var (slot, mesh) in bound)
                mesh.Draw.VertexBuffers[1] = Binding(slot);
        }

        public VertexBufferBinding Binding(int slot)
            => new(Output, Shared.OutputDeclaration, Shared.Binding.Count, Shared.OutputVertexStride, slot * SlotBytes);

        public int Allocate()
        {
            int slot = Array.IndexOf(occupied, false);
            occupied[slot] = true;
            Used++;
            return slot;
        }

        public void Bind(int slot, Mesh mesh) => bound[slot] = mesh;

        public void Free(int slot)
        {
            occupied[slot] = false;
            flagData[slot] = 0;
            bound.Remove(slot);
            Used--;
        }

        public void Set(int slot, float[] values, int pose, in Matrix inverseWorld, bool skinned, bool morphed, int activeCount)
        {
            values.AsSpan().CopyTo(weightData.AsSpan(slot * Shared.TargetCount, Shared.TargetCount));
            flagData[slot] = InstanceActive | (skinned ? InstanceSkinned : 0) | (morphed ? InstanceMorphed : 0);
            poseData[slot] = (uint)Math.Max(pose, 0);
            WriteColumns(inverseWorld, inverseWorlds.AsSpan(slot * RowsPerMatrix));
            SlotCount = Math.Max(SlotCount, slot + 1);
            ActiveCount = Math.Max(ActiveCount, activeCount);
        }

        // Appends this frame's records and weights of the used slots. Slots not prepared this frame are inactive.
        public void Write(FrameArena<Vector4> records, FrameArena<float> weights)
        {
            RecordBase = records.Allocate(SlotCount * RowsPerRecord);
            var rows = records.Span(RecordBase, SlotCount * RowsPerRecord);
            for (int slot = 0; slot < SlotCount; slot++)
            {
                var record = rows.Slice(slot * RowsPerRecord, RowsPerRecord);
                record[0] = new Vector4(BitConverter.UInt32BitsToSingle(flagData[slot]), BitConverter.UInt32BitsToSingle(poseData[slot]), 0, 0);
                inverseWorlds.AsSpan(slot * RowsPerMatrix, RowsPerMatrix).CopyTo(record.Slice(1));
            }
            WeightBase = weights.Allocate(SlotCount * Shared.TargetCount);
            weightData.AsSpan(0, SlotCount * Shared.TargetCount).CopyTo(weights.Span(WeightBase, SlotCount * Shared.TargetCount));
        }

        // Slots not prepared next frame keep their last output and are skipped by the dispatch.
        public void EndFrame()
        {
            Array.Clear(flagData);
            SlotCount = 0;
            ActiveCount = 0;
        }

        public void Dispose()
        {
            Output?.Dispose();
        }
    }

    // Per-frame data appended by all batches and uploaded with one call. The buffer grows by doubling and is kept.
    private sealed class FrameArena<T> : IDisposable where T : unmanaged
    {
        private T[] stagingValues = new T[1];
        private int count;
        public Buffer Buffer;

        public int Allocate(int length)
        {
            int offset = count;
            count += length;
            if (count > stagingValues.Length)
                Array.Resize(ref stagingValues, Math.Max(count, stagingValues.Length * 2));
            return offset;
        }

        public Span<T> Span(int offset, int length) => stagingValues.AsSpan(offset, length);

        public void Upload(GraphicsDevice device, CommandList command)
        {
            if (Buffer == null || Buffer.ElementCount < stagingValues.Length)
            {
                Buffer?.Dispose();
                Buffer = Buffer.New(device, stagingValues.Length * Unsafe.SizeOf<T>(), Unsafe.SizeOf<T>(), BufferFlags.StructuredBuffer | BufferFlags.ShaderResource, PixelFormat.None);
            }
            if (count != 0)
                Buffer.SetData(command, (ReadOnlySpan<T>)stagingValues.AsSpan(0, count));
            count = 0;
        }

        public void Dispose() => Buffer?.Dispose();
    }

    private sealed class Instance : IDisposable
    {
        public readonly SharedMesh Shared;
        public readonly Batch Batch;
        public readonly int Slot;
        public readonly Mesh Mesh;
        public readonly MeshMorphBuffers Buffers;
        public readonly float[] ZeroWeights;
        private readonly int boneCount;
        private float[] weights;
        private Matrix world;
        private bool morphed;

        public bool IsScheduled { get; private set; }

        public bool IsSkinned { get; private set; }

        public Instance(GraphicsDevice device, SharedMesh shared, Batch batch)
        {
            Shared = shared;
            Batch = batch;
            boneCount = shared.BoneCount;
            ZeroWeights = new float[shared.TargetCount];
            if (batch == null)
            {
                Buffers = new MeshMorphBuffers
                {
                    Entries = shared.Entries,
                    VertexOffsets = shared.Offsets,
                    Weights = Buffer.New(device, Math.Max(shared.TargetCount, 1) * sizeof(float), sizeof(float), BufferFlags.StructuredBuffer | BufferFlags.ShaderResource, PixelFormat.None),
                    Flags = shared.MorphFlags,
                    TargetCount = shared.TargetCount,
                };
                return;
            }
            Slot = batch.Allocate();
            var source = shared.Source;
            Mesh = new Mesh(source)
            {
                Skinning = null,
                Draw = new MeshDraw
                {
                    PrimitiveType = source.Draw.PrimitiveType,
                    DrawCount = source.Draw.DrawCount,
                    StartLocation = source.Draw.StartLocation,
                    IndexBuffer = source.Draw.IndexBuffer,
                    VertexBuffers = [shared.SourceStream, batch.Binding(Slot)]
                }
            };
            batch.Bind(Slot, Mesh);
        }

        // Keeps this frame's deformation until a view collects the model.
        public void Schedule(float[] values, Matrix[] bones, Matrix world, bool morphed)
        {
            if (bones.Length > boneCount)
                throw new InvalidOperationException("Skinning bone layout changed.");
            weights = values;
            this.world = world;
            this.morphed = morphed;
            IsSkinned = bones.Length != 0;
            IsScheduled = true;
        }

        // Writes the scheduled deformation into the batch; vertex shader instances upload their weights in Upload.
        public void Prepare(int pose)
        {
            IsScheduled = false;
            if (Batch == null)
                return;
            int activeCount = 0;
            foreach (var value in weights)
            {
                if (value != 0)
                    activeCount++;
            }
            var inverseWorld = Matrix.Identity;
            bool worldIsInvertible = Math.Abs(world.Determinant()) >= MinTransformDeterminant;
            bool skinned = IsSkinned && worldIsInvertible;
            if (skinned)
                Matrix.Invert(ref world, out inverseWorld);
            Batch.Set(Slot, weights, pose, inverseWorld, skinned, morphed, activeCount);
        }

        public void Upload(CommandList command)
        {
            if (weights.Length != 0)
                Buffers.Weights.SetData(command, weights.AsSpan());
        }

        public void Dispose()
        {
            Batch?.Free(Slot);
            Buffers?.Weights.Dispose();
        }
    }

    private static class Keys
    {
        private static ObjectParameterKey<Buffer> ObjectKey(string name)
        {
            return ParameterKeys.NewObject<Buffer>(name: "MorphSkinning." + name);
        }

        private static ValueParameterKey<T> ValueKey<T>(string name) where T : struct
        {
            return ParameterKeys.NewValue<T>(name: "MorphSkinning." + name);
        }

        public static readonly ObjectParameterKey<Buffer> Input = ObjectKey("InputVertices");
        public static readonly ObjectParameterKey<Buffer> Entries = ObjectKey("PackedEntries");
        public static readonly ObjectParameterKey<Buffer> Offsets = ObjectKey("VertexOffsets");
        public static readonly ObjectParameterKey<Buffer> Weights = ObjectKey("Weights");
        public static readonly ObjectParameterKey<Buffer> Poses = ObjectKey("PoseRows");
        public static readonly ObjectParameterKey<Buffer> Records = ObjectKey("InstanceRecords");
        public static readonly ObjectParameterKey<Buffer> BoneBinds = ObjectKey("BoneBinds");
        public static readonly ObjectParameterKey<Buffer> BoneNodes = ObjectKey("BoneNodes");
        public static readonly ObjectParameterKey<Buffer> Output = ObjectKey("OutputVertices");

        public static readonly ValueParameterKey<uint> VertexCount = ValueKey<uint>("VertexCount");
        public static readonly ValueParameterKey<uint> Stride = ValueKey<uint>("VertexStride");
        public static readonly ValueParameterKey<uint> Position = ValueKey<uint>("PositionOffset");
        public static readonly ValueParameterKey<uint> BoneCount = ValueKey<uint>("BoneCount");
        public static readonly ValueParameterKey<uint> SkinningFlags = ValueKey<uint>("SkinningFlags");
        public static readonly ValueParameterKey<uint> MorphFlags = ValueKey<uint>("MorphFlags");
        public static readonly ValueParameterKey<uint> Index16 = ValueKey<uint>("Index16");
        public static readonly ValueParameterKey<uint> TargetCount = ValueKey<uint>("TargetCount");
        public static readonly ValueParameterKey<uint> ActiveCount = ValueKey<uint>("ActiveCount");
        public static readonly ValueParameterKey<uint> InstanceCount = ValueKey<uint>("InstanceCount");
        public static readonly ValueParameterKey<uint> RecordBase = ValueKey<uint>("RecordBase");
        public static readonly ValueParameterKey<uint> WeightBase = ValueKey<uint>("WeightBase");
        public static readonly ValueParameterKey<uint> OutputStride = ValueKey<uint>("OutputStride");
        public static readonly ValueParameterKey<uint> OutputVertexStride = ValueKey<uint>("OutputVertexStride");
        public static readonly ValueParameterKey<uint> TangentW = ValueKey<uint>("TangentW");

        public static readonly ValueParameterKey<int> Normal = ValueKey<int>("NormalOffset");
        public static readonly ValueParameterKey<int> Tangent = ValueKey<int>("TangentOffset");
        public static readonly ValueParameterKey<int> Indices = ValueKey<int>("BlendIndicesOffset");
        public static readonly ValueParameterKey<int> BlendWeights = ValueKey<int>("BlendWeightsOffset");
        public static readonly ValueParameterKey<int> OutputNormal = ValueKey<int>("OutputNormalOffset");
        public static readonly ValueParameterKey<int> OutputTangent = ValueKey<int>("OutputTangentOffset");
    }
}
