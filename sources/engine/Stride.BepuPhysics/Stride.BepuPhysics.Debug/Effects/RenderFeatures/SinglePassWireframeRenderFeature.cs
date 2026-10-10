// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.BepuPhysics.Debug.Effects.RenderFeatures;

/// <summary>
/// Draws <see cref="WireFrameRenderObject"/>s as wireframes, in the render stages picked by <see cref="RootRenderFeature.RenderStageSelectors"/>.
/// </summary>
/// <remarks>
/// Lines are depth tested against the scene, but stay visible through the visual model of their own collidable:
/// an id pass records which collidable's model each pixel shows.
/// </remarks>
public class SinglePassWireframeRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance _shader = null!;
    private DynamicEffectInstance _idShader = null!;
    // Indexed by (back faces ? 2 : 0) + (through own model ? 1 : 0), updated once per draw of the feature
    private readonly MutablePipelineState[] _linePipelines = new MutablePipelineState[4];
    private MutablePipelineState _idPipelineState = null!;
    private readonly Dictionary<(VertexDeclaration Layout, PrimitiveType Primitive), PipelineState> _idPipelines = new();
    private EffectBytecode? _idPipelinesBytecode;
    private PixelFormat _idPipelinesDepthFormat;
    private Texture _noIds = null!;
    private Texture? _ids;
    private readonly List<WireFrameRenderObject> _wireframes = new();
    private readonly HashSet<int> _drawnIds = new();
    private readonly HashSet<int> _idsWithModel = new();

    [DataMember(0)]
    public bool Enable = true;

    [DataMember(10)]
    [DataMemberRange(0.0f, 10.0f, 0.001f, 0.002f, 4)]
    public float LineWidth = 3f;

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
        _idPipelineState.State.BlendState = BlendStates.Opaque;

        _noIds = Texture.New2D(Context.GraphicsDevice, 1, 1, PixelFormat.R32_Float, new[] { 0f });
    }

    protected override void Destroy()
    {
        _noIds?.Dispose();
        base.Destroy();
    }

    public override void Prepare(RenderDrawContext context)
    {
        base.Prepare(context);

        // Debug view off: free the id target rather than leave it in the pool until the next resize
        if (RenderObjects.Count == 0 && _ids is not null)
        {
            var ids = _ids;
            context.GraphicsContext.Allocator.Recycle(link => link.Resource == ids);
            _ids = null;
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
        // The bound viewport rather than the view size, so edges line up with SV_Position when the view is offset (VR eye, letterboxing)
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.Viewport, new Vector4(viewport.Width, viewport.Height, viewport.X, viewport.Y));
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.LineWidth, LineWidth);
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.ModelIds, ids ?? _noIds);
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
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.LineColor, (Vector3)wireframe.Color);
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.ObjectId, (float)wireframe.ObjectId);
            // A collidable without a visual model has nothing to be drawn through
            var hasModel = ids is not null && _idsWithModel.Contains(wireframe.ObjectId);

            // Front faces, then the ones their own model hides; back faces where nothing hides them
            DrawPass(context, wireframe, throughOwnModel: false, backFaces: false);
            if (hasModel)
                DrawPass(context, wireframe, throughOwnModel: true, backFaces: false);
            DrawPass(context, wireframe, throughOwnModel: false, backFaces: true);
        }

        if (ids is not null)
            context.GraphicsContext.Allocator.ReleaseReference(ids);
    }

    private void DrawPass(RenderDrawContext context, WireFrameRenderObject wireframe, bool throughOwnModel, bool backFaces)
    {
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.ThroughOwnModel, throughOwnModel ? 1f : 0f);

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
    /// each pixel gets the id of the collidable whose model it shows.
    /// </summary>
    /// <returns> The target, to release once drawn with, or null when the depth buffer cannot be shared (multisampled, or none bound) </returns>
    private Texture? DrawModelIds(RenderDrawContext context, RenderView renderView, Viewport viewport)
    {
        var commandList = context.CommandList;
        var depth = commandList.DepthStencilBuffer;
        if (depth is null || depth.MultisampleCount != MultisampleCount.None)
            return null;

        var targets = commandList.RenderTargets.ToArray();
        var ids = _ids = context.GraphicsContext.Allocator.GetTemporaryTexture2D(depth.Width, depth.Height, PixelFormat.R32_Float);
        commandList.ResourceBarrierTransition(ids, BarrierLayout.RenderTarget);
        commandList.Clear(ids, new Color4(0));
        commandList.SetRenderTargets(depth, [ids]);
        commandList.SetViewport(viewport);

        _idShader.UpdateEffect(context.GraphicsDevice);
        if (_idPipelinesBytecode != _idShader.Effect.Bytecode || _idPipelinesDepthFormat != depth.ViewFormat)
        {
            _idPipelines.Clear();
            _idPipelinesBytecode = _idShader.Effect.Bytecode;
            _idPipelinesDepthFormat = depth.ViewFormat;
        }
        _idShader.Parameters.Set(TransformationKeys.ViewProjection, renderView.ViewProjection);
        _idsWithModel.Clear();
        _drawnIds.Clear();
        foreach (var wireframe in _wireframes)
        {
            if (wireframe.Owner is not { } owner || !_drawnIds.Add(wireframe.ObjectId))
                continue;

            _idShader.Parameters.Set(BepuDebugIdShaderKeys.ObjectId, (float)wireframe.ObjectId);
            if (DrawOwnModels(context, owner, owner))
                _idsWithModel.Add(wireframe.ObjectId);
        }

        commandList.SetRenderTargets(depth, targets);
        commandList.SetViewport(viewport);
        commandList.ResourceBarrierTransition(ids, BarrierLayout.ShaderResource);
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
            foreach (var mesh in model.Meshes)
            {
                var world = nodes is not null && mesh.NodeIndex < nodes.Length ? nodes[mesh.NodeIndex].WorldMatrix : entity.Transform.WorldMatrix;
                drawn |= DrawMeshId(context, mesh, world);
            }
        }

        foreach (var child in entity.Transform.Children)
            drawn |= DrawOwnModels(context, child.Entity, owner);
        return drawn;
    }

    private bool DrawMeshId(RenderDrawContext context, Mesh mesh, Matrix world)
    {
        var draw = mesh.Draw;
        if (draw?.VertexBuffers is not { Length: > 0 } vertexBuffers)
            return false;

        _idShader.Parameters.Set(TransformationKeys.World, world);
        // Meshes of one vertex buffer share their pipeline state by layout; others build theirs each time
        var key = (vertexBuffers[0].Declaration, draw.PrimitiveType);
        if (vertexBuffers.Length > 1 || !_idPipelines.TryGetValue(key, out var pipelineState))
        {
            _idPipelineState.State.RootSignature = _idShader.RootSignature;
            _idPipelineState.State.EffectBytecode = _idShader.Effect.Bytecode;
            _idPipelineState.State.InputElements = vertexBuffers.CreateInputElements();
            _idPipelineState.State.PrimitiveType = draw.PrimitiveType;
            _idPipelineState.State.Output.CaptureState(context.CommandList);
            _idPipelineState.Update();
            pipelineState = _idPipelineState.CurrentState!;
            if (vertexBuffers.Length == 1)
                _idPipelines.Add(key, pipelineState);
        }

        var commandList = context.CommandList;
        for (int i = 0; i < vertexBuffers.Length; i++)
            commandList.SetVertexBuffer(i, vertexBuffers[i].Buffer, vertexBuffers[i].Offset, vertexBuffers[i].Stride);
        commandList.SetPipelineState(pipelineState);
        _idShader.Apply(context.GraphicsContext);
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
}
