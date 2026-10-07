// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Debug.Effects.RenderFeatures;

/// <summary>
/// Draws <see cref="LineRenderObject"/>s as screen-space lines of constant width.
/// </summary>
public class LineRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance _shader = null!;
    private MutablePipelineState _pipelineState = null!;
    private readonly List<LineRenderObject> _lines = new();

    /// <summary>
    /// The width of the lines, in pixels.
    /// </summary>
    [DataMember(10)]
    [DataMemberRange(0.0f, 10.0f, 0.001f, 0.002f, 4)]
    public float LineWidth = 3f;

    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(LineRenderObject);

    public LineRenderFeature()
    {
        SortKey = 255;
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();

        _shader = new DynamicEffectInstance("StrideDebugLineShader");
        _shader.Initialize(Context.Services);

        _pipelineState = new MutablePipelineState(Context.GraphicsDevice);
        _pipelineState.State.SetDefaults();
        _pipelineState.State.InputElements = LineRenderObject.LineVertex.Layout.CreateInputElements();
        _pipelineState.State.PrimitiveType = PrimitiveType.LineList;
        _pipelineState.State.RasterizerState.CullMode = CullMode.None;
    }

    protected override void OnAddRenderObject(RenderObject renderObject)
    {
        base.OnAddRenderObject(renderObject);
        _lines.Add((LineRenderObject)renderObject);
    }

    protected override void OnRemoveRenderObject(RenderObject renderObject)
    {
        base.OnRemoveRenderObject(renderObject);
        _lines.Remove((LineRenderObject)renderObject);
    }

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
    {
        _shader.UpdateEffect(context.GraphicsDevice);
        _shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderView.ViewProjection);
        _shader.Parameters.Set(DebugLineShaderKeys.ViewportSize, renderView.ViewSize);
        _shader.Parameters.Set(DebugLineShaderKeys.LineWidth, LineWidth);

        foreach (var lines in _lines)
        {
            if (!lines.Enabled || lines.VertexCount == 0)
                continue;

            var vertexBuffer = lines.Upload(context.CommandList);

            _pipelineState.State.RootSignature = _shader.RootSignature;
            _pipelineState.State.EffectBytecode = _shader.Effect.Bytecode;
            _pipelineState.State.Output.CaptureState(context.CommandList);
            _pipelineState.Update();

            context.CommandList.SetVertexBuffer(0, vertexBuffer, 0, LineRenderObject.LineVertex.Size);
            context.CommandList.SetPipelineState(_pipelineState.CurrentState);
            _shader.Apply(context.GraphicsContext);
            context.CommandList.Draw(lines.VertexCount);
        }
    }
}
