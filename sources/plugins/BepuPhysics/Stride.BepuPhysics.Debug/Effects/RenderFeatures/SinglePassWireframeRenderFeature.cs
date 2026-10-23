// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Debug.Effects.RenderFeatures;

/// <summary>
/// Draws <see cref="WireFrameRenderObject"/>s as wireframes, in the render stages picked by <see cref="RootRenderFeature.RenderStageSelectors"/>.
/// </summary>
public class SinglePassWireframeRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance _shader = null!;
    private MutablePipelineState _pipelineState = null!;

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

        // create the pipeline state and set properties that won't change
        _pipelineState = new MutablePipelineState(Context.GraphicsDevice);
        _pipelineState.State.SetDefaults();
        _pipelineState.State.InputElements = VertexPosition3.Layout.CreateInputElements();
        _pipelineState.State.BlendState = BlendStates.AlphaBlend;
        _pipelineState.State.RasterizerState.CullMode = CullMode.None;
    }

    public override void Prepare(RenderDrawContext context)
    {
        base.Prepare(context);
    }

    public void IsEnabled(bool enable)
    {
        Enable = enable;
    }

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (!Enable || startIndex == endIndex) return;

        _shader.UpdateEffect(context.GraphicsDevice);
        _shader.Parameters.Set(TransformationKeys.WorldScale, new Vector3(1.002f));
        // The bound viewport rather than the view size, so edges line up with SV_Position when the view is offset (VR eye, letterboxing)
        var viewport = context.CommandList.Viewport;
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.Viewport, new Vector4(viewport.Width, viewport.Height, viewport.X, viewport.Y));
        _shader.Parameters.Set(SinglePassWireframeShaderKeys.LineWidth, LineWidth);

        for (int index = startIndex; index < endIndex; index++)
        {
            var myRenderObject = (WireFrameRenderObject)GetRenderNode(renderViewStage.SortedRenderNodes[index].RenderNode).RenderObject;

            // set shader parameters
            _shader.Parameters.Set(TransformationKeys.WorldViewProjection, myRenderObject.WorldMatrix * renderView.ViewProjection); // matrix
            _shader.Parameters.Set(SinglePassWireframeShaderKeys.LineColor, (Vector3)myRenderObject.Color);

            // prepare pipeline state
            _pipelineState.State.RootSignature = _shader.RootSignature;
            _pipelineState.State.EffectBytecode = _shader.Effect.Bytecode;
            _pipelineState.State.PrimitiveType = myRenderObject.PrimitiveType;

            _pipelineState.State.Output.CaptureState(context.CommandList);
            _pipelineState.Update();

            context.CommandList.SetVertexBuffer(0, myRenderObject.VertexBuffer, 0, myRenderObject.VertexStride);
            context.CommandList.SetIndexBuffer(myRenderObject.IndexBuffer, 0, true);
            context.CommandList.SetPipelineState(_pipelineState.CurrentState);

            // apply the effect
            _shader.Apply(context.GraphicsContext);

            context.CommandList.DrawIndexed(myRenderObject.IndexBuffer.ElementCount);
        }
    }
}
