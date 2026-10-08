// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

using Xunit;

using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics.Tests;

public class TestPixelShaderUnorderedAccess : GraphicTestGameBase
{
    public TestPixelShaderUnorderedAccess()
    {
        // Test shaders compile at the test settings' profile otherwise, which has no pixel shader UAVs
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
    }

    /// <summary>
    /// A resource set with <see cref="DescriptorSet.SetValue"/> in a UAV slot, as material and render feature
    /// parameters are, is bound as a UAV: the pixel shader's write to the RWBuffer lands.
    /// </summary>
    [SkippableFact]
    public void PixelShaderWritesToRWBufferSetThroughSetValue()
    {
        PerformTest(game =>
        {
            // Vulkan needs fragmentStoresAndAtomics enabled at device creation
            Skip.If(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Pixel shader UAVs are not enabled on Vulkan yet.");

            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;
            var output = Buffer.Typed.New(device, 1, PixelFormat.R32_UInt, unorderedAccess: true);
            var target = Texture.New2D(device, 8, 8, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);

            var effect = new EffectInstance(game.EffectSystem.LoadEffect("PixelShaderUnorderedAccessEffect").WaitForResult());
            effect.UpdateEffect(device);

            var layouts = effect.DescriptorReflection.Layouts.Select(layout => layout.Layout).ToArray();
            var pool = DescriptorPool.New(device, [new DescriptorTypeCount(EffectParameterClass.UnorderedAccessView, 8)]);
            var descriptorSets = new DescriptorSet[layouts.Length];
            for (int i = 0; i < layouts.Length; i++)
            {
                descriptorSets[i] = DescriptorSet.New(device, pool, DescriptorSetLayout.New(device, layouts[i]));
                for (int slot = 0; slot < layouts[i].Entries.Count; slot++)
                {
                    if (layouts[i].Entries[slot].Class == EffectParameterClass.UnorderedAccessView)
                        descriptorSets[i].SetValue(slot, output);
                }
            }

            var pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();
            pipelineState.State.RootSignature = effect.RootSignature;
            pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
            pipelineState.State.InputElements = PrimitiveQuad.VertexDeclaration.CreateInputElements();
            pipelineState.State.PrimitiveType = PrimitiveQuad.PrimitiveType;

            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();
            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetDescriptorSets(0, descriptorSets);
            device.PrimitiveQuad.Draw(commandList);

            Assert.Equal(0xC0FFEEu, output.GetData<uint>(commandList)[0]);
        });
    }
}
