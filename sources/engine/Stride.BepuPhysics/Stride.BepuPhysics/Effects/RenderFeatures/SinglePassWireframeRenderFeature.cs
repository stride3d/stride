using System.Linq;
using Silk.NET.OpenGL;
using Stride.BepuPhysics.Components.Colliders;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Streaming;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Effects.RenderFeatures;

public class SinglePassWireframeRenderFeature : RootRenderFeature
{
    DynamicEffectInstance shader;
    MutablePipelineState pipelineState;

	[DataMember(0)]
	public bool Enable = true;

    [DataMember(10)]
	[DataMemberRange(0.0f, 10.0f, 0.001f, 0.002f, 4)]
	public float LineWidth = 3f;
    

    public override Type SupportedRenderObjectType => typeof(WireFrameRenderObject);

    private List<WireFrameRenderObject> _wireframes = new(); 

    public SinglePassWireframeRenderFeature()
    {
        SortKey = 255;
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();

        // initialize shader
        shader = new DynamicEffectInstance("SinglePassWireframeShader");
        shader.Initialize(Context.Services);

        // create the pipeline state and set properties that won't change
        pipelineState = new MutablePipelineState(Context.GraphicsDevice);
        pipelineState.State.SetDefaults();
        pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
        pipelineState.State.BlendState = BlendStates.AlphaBlend;
        pipelineState.State.RasterizerState.CullMode = CullMode.None;
    }

    protected override void OnAddRenderObject(RenderObject renderObject)
    {
        base.OnAddRenderObject(renderObject);
        _wireframes.Add((WireFrameRenderObject)renderObject);
    }

    protected override void OnRemoveRenderObject(RenderObject renderObject)
    {
        base.OnRemoveRenderObject(renderObject);
        _wireframes.Remove((WireFrameRenderObject)renderObject);
    }

    public override void Prepare(RenderDrawContext context)
    {
        base.Prepare(context); //shapeData.Value.Points.Select(e => new VertexPositionNormalTexture(e, normal, texturePos)).ToArray()
    }

    public void IsEnabled(bool enable)
    {
		Enable = enable;
	}

    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
    {
        if (!Enable) return;

        shader.UpdateEffect(context.GraphicsDevice);
        shader.Parameters.Set(TransformationKeys.WorldScale, new Vector3(1.002f));
        shader.Parameters.Set(SinglePassWireframeShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
        shader.Parameters.Set(SinglePassWireframeShaderKeys.LineWidth, LineWidth);

        foreach (var myRenderObject in _wireframes)
        {
            // set shader parameters
            shader.Parameters.Set(TransformationKeys.WorldViewProjection, myRenderObject.WorldMatrix * renderView.ViewProjection); // matrix
            shader.Parameters.Set(SinglePassWireframeShaderKeys.LineColor, (Vector3)myRenderObject.Color);

            // prepare pipeline state
            pipelineState.State.RootSignature = shader.RootSignature;
            pipelineState.State.EffectBytecode = shader.Effect.Bytecode;
            pipelineState.State.PrimitiveType = myRenderObject.PrimitiveType;

            pipelineState.State.Output.CaptureState(context.CommandList);
            pipelineState.Update();

            context.CommandList.SetVertexBuffer(0, myRenderObject.VertexBuffer, 0, VertexPositionNormalTexture.Layout.VertexStride);
            context.CommandList.SetIndexBuffer(myRenderObject.IndiceBuffer, 0, true);
            context.CommandList.SetPipelineState(pipelineState.CurrentState);

            // apply the effect
            shader.Apply(context.GraphicsContext);

            context.CommandList.DrawIndexed(myRenderObject.IndiceBuffer.ElementCount);
        }
    }
}
