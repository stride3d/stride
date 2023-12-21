using Stride.BepuPhysics.Components.Colliders;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Effects.RenderFeatures;

public class SinglePassWireframeRenderFeature : RootRenderFeature
{
	DynamicEffectInstance shader;
	MutablePipelineState pipelineState;

	/// <summary>
	/// Adjust scale a bit of wireframe model to prevent z-fighting
	/// </summary>
	[DataMember(10)]
	[DataMemberRange(0.0f, 0.1f, 0.001f, 0.002f, 4)]
	public float ScaleAdjust = 0.001f;

	public override Type SupportedRenderObjectType => typeof(RenderMesh);

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

	public override void Prepare(RenderDrawContext context)
	{
		base.Prepare(context);
	}

	public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
	{
		shader.UpdateEffect(context.GraphicsDevice);

		foreach (var renderNode in renderViewStage.SortedRenderNodes)
		{
			var renderMesh = renderNode.RenderObject as RenderMesh;
			if (renderMesh == null)
			{
				continue;
			}

			// get wireframe script
			//WireframeScript wireframeScript = null;
			//if (renderMesh.Source is ModelComponent)
			//{
			//	wireframeScript = (renderMesh.Source as ModelComponent).Entity.Get<WireframeScript>();
			//}
			//
			//if (wireframeScript == null || !wireframeScript.Enabled)
			//{
			//	continue;
			//}
			// get collider data and pass the buffers to the shader
			var collider = renderMesh.Source as ColliderComponent;

			MeshDraw drawData = renderMesh.ActiveMeshDraw;

			// bind VB
			for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
			{
				var vertexBuffer = drawData.VertexBuffers[slot];
				context.CommandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
			}

			// set shader parameters
			shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * renderView.ViewProjection); // matrix
			shader.Parameters.Set(TransformationKeys.WorldScale, new Vector3(ScaleAdjust + 1.0f)); // increase size to avoid z-fight
			shader.Parameters.Set(SinglePassWireframeShaderKeys.Viewport, new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0));
			shader.Parameters.Set(SinglePassWireframeShaderKeys.LineWidth, 2f);
			shader.Parameters.Set(SinglePassWireframeShaderKeys.LineColor, (Vector3)Color.Red);

			// prepare pipeline state
			pipelineState.State.RootSignature = shader.RootSignature;
			pipelineState.State.EffectBytecode = shader.Effect.Bytecode;
			pipelineState.State.PrimitiveType = drawData.PrimitiveType;

			pipelineState.State.Output.CaptureState(context.CommandList);
			pipelineState.Update();

			context.CommandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
			context.CommandList.SetPipelineState(pipelineState.CurrentState);

			// apply the effect
			shader.Apply(context.GraphicsContext);

			if (drawData.IndexBuffer != null)
			{
				context.CommandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
			}
			else
			{
				context.CommandList.Draw(drawData.DrawCount, drawData.StartLocation);
			}
		}
	}
}
