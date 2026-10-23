// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace Stride.BepuPhysics.Debug.Effects.RenderFeatures;

/// <summary>
/// Draws <see cref="WireFrameRenderObject"/>s as wireframes, in the render stages picked by <see cref="RootRenderFeature.RenderStageSelectors"/>.
/// </summary>
/// <remarks>
/// Lines are depth tested against the scene, but stay visible through the visual model of their own collidable:
/// an id pass records which collidable's model each pixel shows. Lines farther than the debug mesh's error from their
/// own model are colored <see cref="InsideColor"/> when the model hides them, <see cref="OutsideColor"/> when they stand out of it.
/// </remarks>
public class SinglePassWireframeRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance _shader = null!;
    private DynamicEffectInstance _idShader = null!;
    private DynamicEffectInstance _idSkinnedShader = null!;
    // Size of BoneMatrices in BepuDebugIdSkinnedShader, set as its SkinningMaxBones macro; a mesh with more bones is left out
    private const int MaxBones = 128;
    // Indexed by (back faces ? 2 : 0) + (through own model ? 1 : 0), updated once per draw of the feature
    private readonly MutablePipelineState[] _linePipelines = new MutablePipelineState[4];
    private MutablePipelineState _idPipelineState = null!;
    private MutablePipelineState _idBackPipelineState = null!;
    private MutablePipelineState[] _idPasses = null!;
    private MutablePipelineState _idPass = null!;
    private readonly Dictionary<(MutablePipelineState Pass, bool Skinned, VertexDeclaration Layout, PrimitiveType Primitive), PipelineState> _idPipelines = new();
    private (EffectBytecode?, EffectBytecode?) _idPipelinesBytecode;
    private PixelFormat _idPipelinesDepthFormat;
    private Texture _noIds = null!;
    private Texture? _ids;
    private Texture? _normals;
    private readonly List<WireFrameRenderObject> _wireframes = new();
    private readonly HashSet<int> _drawnIds = new();
    private readonly HashSet<int> _idsWithModel = new();

    [DataMember(0)]
    public bool Enable = true;

    [DataMember(10)]
    [DataMemberRange(0.0f, 10.0f, 0.001f, 0.002f, 4)]
    public float LineWidth = 3f;

    /// <summary> Draws the back faces of the colliders as dashed lines, hidden by other objects but visible through their own model </summary>
    [DataMember(20)]
    public bool ShowBackFaces;

    /// <summary> Color of the lines that sit inside their own visual model by more than the debug mesh's error </summary>
    [DataMember(30)]
    public Color3 InsideColor = new(0.941f, 0.894f, 0.259f);

    /// <summary> Color of the lines that stand out of their own visual model by more than the debug mesh's error </summary>
    [DataMember(40)]
    public Color3 OutsideColor = new(0.835f, 0.369f, 0f);

    public override Type SupportedRenderObjectType => typeof(WireFrameRenderObject);

    public SinglePassWireframeRenderFeature()
    {
        SortKey = 254; // Below LineRenderFeature, so contacts are drawn over the wireframes
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();

        // initialize shader
        _shader = new DynamicEffectInstance("StrideSinglePassWireframeShader");
        _shader.Initialize(Context.Services);
        _idShader = new DynamicEffectInstance("StrideBepuDebugIdShader");
        _idShader.Initialize(Context.Services);
        _idSkinnedShader = new DynamicEffectInstance("StrideBepuDebugIdSkinnedShader");
        _idSkinnedShader.Initialize(Context.Services);
        _idSkinnedShader.Parameters.Set(MaterialKeys.SkinningMaxBones, MaxBones);

        // create the pipeline states and set properties that won't change
        for (int i = 0; i < _linePipelines.Length; i++)
        {
            var pipeline = _linePipelines[i] = new MutablePipelineState(Context.GraphicsDevice);
            pipeline.State.SetDefaults();
            pipeline.State.InputElements = VertexPosition3.Layout.CreateInputElements();
            pipeline.State.BlendState = BlendStates.AlphaBlend;
            pipeline.State.RasterizerState.CullMode = (i & 2) != 0 ? CullMode.Front : CullMode.Back;
            pipeline.State.DepthStencilState = (i & 1) != 0 ? DepthStencilStates.None : DepthStencilStates.Default;
        }

        _idPipelineState = new MutablePipelineState(Context.GraphicsDevice);
        _idPipelineState.State.SetDefaults();
        _idPipelineState.State.RasterizerState.CullMode = CullMode.Back;
        _idPipelineState.State.DepthStencilState = DepthStencilStates.DepthRead;
        // The scene drew these surfaces already: the bias keeps them passing where both depths are equal.
        // No slope bias, it would let faces seen edge-on win over the surface in front of them
        _idPipelineState.State.RasterizerState.DepthBias = -2;
        var front = BlendStates.Opaque;
        front.IndependentBlendEnable = true;
        front.RenderTargets[0].ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;
        front.RenderTargets[1].ColorWriteChannels = ColorWriteChannels.All;
        _idPipelineState.State.BlendState = front;

        // Back faces behind the scene keep, in alpha, the nearest one: where the model the pixel shows ends along the view ray
        _idBackPipelineState = new MutablePipelineState(Context.GraphicsDevice);
        _idBackPipelineState.State.SetDefaults();
        _idBackPipelineState.State.RasterizerState.CullMode = CullMode.Front;
        _idBackPipelineState.State.DepthStencilState = new DepthStencilStateDescription(depthEnable: true, depthWriteEnable: false) { DepthBufferFunction = CompareFunction.Greater };
        var back = BlendStates.Opaque;
        back.IndependentBlendEnable = true;
        back.RenderTargets[1].ColorWriteChannels = ColorWriteChannels.None;
        back.RenderTargets[0].BlendEnable = true;
        back.RenderTargets[0].AlphaSourceBlend = Blend.One;
        back.RenderTargets[0].AlphaDestinationBlend = Blend.One;
        back.RenderTargets[0].AlphaBlendFunction = BlendFunction.Max;
        back.RenderTargets[0].ColorWriteChannels = ColorWriteChannels.Alpha;
        _idBackPipelineState.State.BlendState = back;
        _idPasses = [_idPipelineState, _idBackPipelineState];

        _noIds = Texture.New2D(Context.GraphicsDevice, 1, 1, PixelFormat.R32G32B32A32_Float, new[] { new Vector4(0) });
    }

    protected override void Destroy()
    {
        _noIds?.Dispose();
        base.Destroy();
    }

    public override void Prepare(RenderDrawContext context)
    {
        base.Prepare(context);

        // Debug view off: free the id targets rather than leave them in the pool until the next resize
        if (RenderObjects.Count == 0 && _ids is not null)
        {
            var (ids, normals) = (_ids, _normals);
            context.GraphicsContext.Allocator.Recycle(link => link.Resource == ids || link.Resource == normals);
            _ids = _normals = null;
        }
    }

    public void IsEnabled(bool enable)
    {
        Enable = enable;
    }

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (!Enable || startIndex == endIndex) return;

        _wireframes.Clear();
        for (int index = startIndex; index < endIndex; index++)
            _wireframes.Add((WireFrameRenderObject)GetRenderNode(renderViewStage.SortedRenderNodes[index].RenderNode).RenderObject);

        var commandList = context.CommandList;
        var viewport = commandList.Viewport;
        var ids = DrawModelIds(context, renderView, viewport);

        _shader.UpdateEffect(context.GraphicsDevice);
        _shader.Parameters.Set(TransformationKeys.Projection, renderView.Projection);
        // The bound viewport rather than the view size, so edges line up with SV_Position when the view is offset (VR eye, letterboxing)
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.Viewport, new Vector4(viewport.Width, viewport.Height, viewport.X, viewport.Y));
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.LineWidth, LineWidth);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.InsideColor, (Vector3)InsideColor);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.OutsideColor, (Vector3)OutsideColor);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.ModelIds, ids ?? _noIds);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.ModelNormals, ids is null ? _noIds : _normals);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.ProjectionScale, renderView.Projection.M22 * viewport.Height * 0.5f);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.Orthographic, renderView.Projection.M44 == 1f ? 1f : 0f);
        foreach (var pipeline in _linePipelines)
        {
            pipeline.State.RootSignature = _shader.RootSignature;
            pipeline.State.EffectBytecode = _shader.Effect.Bytecode;
            pipeline.State.PrimitiveType = PrimitiveType.TriangleList;
            pipeline.State.Output.CaptureState(commandList);
            pipeline.Update();
        }

        foreach (var wireframe in _wireframes)
        {
            _shader.Parameters.Set(TransformationKeys.WorldViewProjection, wireframe.WorldMatrix * renderView.ViewProjection);
            _shader.Parameters.Set(TransformationKeys.WorldView, wireframe.WorldMatrix * renderView.View);
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.LineColor, (Vector3)wireframe.Color);
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.ObjectId, (float)wireframe.ObjectId);
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.Threshold, Threshold(wireframe));
            // A collidable without a visual model has nothing to be compared to, nor to be drawn through
            var hasModel = ids is not null && _idsWithModel.Contains(wireframe.ObjectId);
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.CompareToModel, hasModel ? 1f : 0f);

            // Front faces, classified, then the ones their own model hides; back faces where nothing hides them,
            // and when asked for, dashed and through their own model too
            DrawPass(context, wireframe, throughOwnModel: false, backFaces: false);
            if (hasModel)
                DrawPass(context, wireframe, throughOwnModel: true, backFaces: false);
            DrawPass(context, wireframe, throughOwnModel: false, backFaces: true);
            if (ShowBackFaces && hasModel)
                DrawPass(context, wireframe, throughOwnModel: true, backFaces: true);
        }

        if (ids is not null)
        {
            context.GraphicsContext.Allocator.ReleaseReference(ids);
            context.GraphicsContext.Allocator.ReleaseReference(_normals!);
        }
    }

    private void DrawPass(RenderDrawContext context, WireFrameRenderObject wireframe, bool throughOwnModel, bool backFaces)
    {
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.ThroughOwnModel, throughOwnModel ? 1f : 0f);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.Dotted, backFaces && ShowBackFaces ? 1f : 0f);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.Classify, backFaces ? 0f : 1f);

        var pipeline = _linePipelines[(backFaces ? 2 : 0) + (throughOwnModel ? 1 : 0)];
        if (pipeline.State.PrimitiveType != wireframe.PrimitiveType)
        {
            pipeline.State.PrimitiveType = wireframe.PrimitiveType;
            pipeline.Update();
        }

        context.CommandList.SetVertexBuffer(0, wireframe.VertexBuffer, 0, wireframe.VertexStride);
        context.CommandList.SetIndexBuffer(wireframe.IndexBuffer, 0, true);
        context.CommandList.SetPipelineState(pipeline.CurrentState);
        _shader.Apply(context.GraphicsContext);
        context.CommandList.DrawIndexed(wireframe.IndexBuffer.ElementCount);
    }

    /// <summary>
    /// Draws the visual models of the collidables into a temporary target, against the scene's depth without writing it:
    /// each pixel gets the id of the collidable whose model it shows, the view depth and facet error of that surface,
    /// and the view depth of the first back face behind it; a second target gets the normal of that facet.
    /// </summary>
    /// <returns> The target, to release once drawn with, or null when the depth buffer cannot be shared (multisampled, or none bound) </returns>
    private Texture? DrawModelIds(RenderDrawContext context, RenderView renderView, Viewport viewport)
    {
        var commandList = context.CommandList;
        var depth = commandList.DepthStencilBuffer;
        if (depth is null || depth.MultisampleCount != MultisampleCount.None)
            return null;

        var targets = commandList.RenderTargets.ToArray();
        var ids = _ids = context.GraphicsContext.Allocator.GetTemporaryTexture2D(depth.Width, depth.Height, PixelFormat.R32G32B32A32_Float);
        _normals = context.GraphicsContext.Allocator.GetTemporaryTexture2D(depth.Width, depth.Height, PixelFormat.R16G16B16A16_Float);
        commandList.ResourceBarrierTransition(ids, BarrierLayout.RenderTarget);
        commandList.ResourceBarrierTransition(_normals, BarrierLayout.RenderTarget);
        commandList.Clear(ids, new Color4(0, 0, 0, float.MinValue));
        commandList.Clear(_normals, new Color4(0));
        commandList.SetRenderTargets(depth, [ids, _normals]);
        commandList.SetViewport(viewport);

        _idShader.UpdateEffect(context.GraphicsDevice);
        _idSkinnedShader.UpdateEffect(context.GraphicsDevice);
        var bytecodes = (_idShader.Effect.Bytecode, _idSkinnedShader.Effect.Bytecode);
        if (_idPipelinesBytecode != bytecodes || _idPipelinesDepthFormat != depth.ViewFormat)
        {
            _idPipelines.Clear();
            _idPipelinesBytecode = bytecodes;
            _idPipelinesDepthFormat = depth.ViewFormat;
        }
        _idShader.Parameters.Set(TransformationKeys.View, renderView.View);
        _idShader.Parameters.Set(TransformationKeys.ViewProjection, renderView.ViewProjection);
        _idSkinnedShader.Parameters.Set(TransformationKeys.View, renderView.View);
        _idSkinnedShader.Parameters.Set(TransformationKeys.ViewProjection, renderView.ViewProjection);
        _idsWithModel.Clear();
        foreach (var pass in _idPasses)
        {
            _idPass = pass;
            _drawnIds.Clear();
            foreach (var wireframe in _wireframes)
            {
                if (wireframe.Owner is not { } owner || !_drawnIds.Add(wireframe.ObjectId))
                    continue;

                _idShader.Parameters.Set(BepuDebugIdShaderKeys.ObjectId, (float)wireframe.ObjectId);
                _idSkinnedShader.Parameters.Set(BepuDebugIdShaderKeys.ObjectId, (float)wireframe.ObjectId);
                if (DrawOwnModels(context, owner, owner))
                    _idsWithModel.Add(wireframe.ObjectId);
            }
        }

        commandList.SetRenderTargets(depth, targets);
        commandList.SetViewport(viewport);
        commandList.ResourceBarrierTransition(ids, BarrierLayout.ShaderResource);
        commandList.ResourceBarrierTransition(_normals, BarrierLayout.ShaderResource);
        return ids;
    }

    /// <summary> The models of a collidable's entity and of its children, down to children that are collidables themselves </summary>
    /// <returns> Whether there was any </returns>
    private bool DrawOwnModels(RenderDrawContext context, Entity entity, Entity owner)
    {
        if (entity != owner && entity.Get<CollidableComponent>() is not null)
            return false;

        var drawn = false;
        foreach (var component in entity.Components)
        {
            if (component is not ModelComponent { Enabled: true, Model: { } model } modelComponent)
                continue;

            var nodes = modelComponent.Skeleton?.NodeTransformations;
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                var mesh = model.Meshes[i];
                var world = nodes is not null && mesh.NodeIndex < nodes.Length ? nodes[mesh.NodeIndex].WorldMatrix : entity.Transform.WorldMatrix;
                // A skinned mesh's vertices are in its bind pose: drawn without its bones, it would stand where the model is not
                var bones = mesh.Skinning is null ? null : i < modelComponent.MeshInfos.Count ? modelComponent.MeshInfos[i].BlendMatrices : null;
                if (mesh.Skinning is not null && (bones is null || bones.Length > MaxBones))
                    continue;
                drawn |= DrawMeshId(context, mesh, world, bones);
            }
        }

        foreach (var child in entity.Transform.Children)
            drawn |= DrawOwnModels(context, child.Entity, owner);
        return drawn;
    }

    private bool DrawMeshId(RenderDrawContext context, Mesh mesh, Matrix world, Matrix[]? bones)
    {
        var draw = mesh.Draw;
        // The facet error comes from the normals; a mesh without them is left out, as if the collidable had no model there
        if (draw?.VertexBuffers is not { Length: > 0 } vertexBuffers || !HasSemantic(vertexBuffers, "NORMAL"))
            return false;
        if (bones is not null && (!HasSemantic(vertexBuffers, "BLENDINDICES") || !HasSemantic(vertexBuffers, "BLENDWEIGHT")))
            return false;

        var shader = bones is null ? _idShader : _idSkinnedShader;
        if (bones is null)
            shader.Parameters.Set(TransformationKeys.World, world);
        else
            shader.Parameters.Set(BepuDebugIdSkinnedShaderKeys.BoneMatrices, bones.Length, ref bones[0]);
        // Meshes of one vertex buffer share their pipeline state by layout; others build theirs each time
        var key = (_idPass, bones is not null, vertexBuffers[0].Declaration, draw.PrimitiveType);
        if (vertexBuffers.Length > 1 || !_idPipelines.TryGetValue(key, out var pipelineState))
        {
            _idPass.State.RootSignature = shader.RootSignature;
            _idPass.State.EffectBytecode = shader.Effect.Bytecode;
            _idPass.State.InputElements = vertexBuffers.CreateInputElements();
            _idPass.State.PrimitiveType = draw.PrimitiveType;
            _idPass.State.Output.CaptureState(context.CommandList);
            _idPass.Update();
            pipelineState = _idPass.CurrentState!;
            if (vertexBuffers.Length == 1)
                _idPipelines.Add(key, pipelineState);
        }

        var commandList = context.CommandList;
        for (int i = 0; i < vertexBuffers.Length; i++)
            commandList.SetVertexBuffer(i, vertexBuffers[i].Buffer, vertexBuffers[i].Offset, vertexBuffers[i].Stride);
        commandList.SetPipelineState(pipelineState);
        shader.Apply(context.GraphicsContext);
        if (draw.IndexBuffer is { } indexBuffer)
        {
            commandList.SetIndexBuffer(indexBuffer.Buffer, indexBuffer.Offset, indexBuffer.Is32Bit);
            commandList.DrawIndexed(draw.DrawCount, draw.StartLocation);
        }
        else
        {
            commandList.Draw(draw.DrawCount, draw.StartLocation);
        }
        return true;
    }

    private static bool HasSemantic(VertexBufferBinding[] vertexBuffers, string semantic)
        => vertexBuffers.Any(b => b.Declaration.VertexElements.Any(e => e.SemanticName == semantic));

    /// <summary> The debug mesh's error in world units, with a margin for depth precision </summary>
    private static float Threshold(WireFrameRenderObject wireframe)
    {
        var world = wireframe.WorldMatrix;
        var scale = MathF.Max(MathF.Max(world.Row1.XYZ().Length(), world.Row2.XYZ().Length()), world.Row3.XYZ().Length());
        var deviation = wireframe.MaxDeviation * scale;
        return deviation * 1.05f + 0.002f;
    }
}
