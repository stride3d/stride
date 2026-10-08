// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Debug.Effects.RenderFeatures;

/// <summary>
/// Draws <see cref="LineRenderObject"/>s as screen-space lines of constant width, on top of the scene.
/// </summary>
/// <remarks> Lines are only drawn in the render stages picked by <see cref="RootRenderFeature.RenderStageSelectors"/>. </remarks>
public class LineRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance _shader = null!;
    private MutablePipelineState _pipelineState = null!;

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
        // A debug overlay: normals start inside body A and markers sit on surfaces, depth testing would hide them
        _pipelineState.State.DepthStencilState = DepthStencilStates.None;
    }

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (!HasLines(renderViewStage, startIndex, endIndex))
            return;

        _shader.UpdateEffect(context.GraphicsDevice);
        _shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderView.ViewProjection);
        _shader.Parameters.Set(DebugLineShaderKeys.ViewportSize, renderView.ViewSize);
        _shader.Parameters.Set(DebugLineShaderKeys.LineWidth, LineWidth);

        for (int index = startIndex; index < endIndex; index++)
        {
            var lines = GetLines(renderViewStage, index);
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

    private LineRenderObject GetLines(RenderViewStage renderViewStage, int index)
        => (LineRenderObject)GetRenderNode(renderViewStage.SortedRenderNodes[index].RenderNode).RenderObject;

    private bool HasLines(RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        for (int index = startIndex; index < endIndex; index++)
        {
            var lines = GetLines(renderViewStage, index);
            if (lines.Enabled && lines.VertexCount > 0)
                return true;
        }
        return false;
    }
}
