// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics.Tests;

/// <summary>
///   Mapping a Dynamic Buffer with <see cref="MapMode.WriteDiscard"/> leaves the draws recorded before it reading the old contents.
/// </summary>
public class TestDynamicBufferDiscard : GraphicTestGameBase
{
    private const int TargetSize = 4;

    // More than 16 vertex buffer slots need Direct3D 11 feature level 10 or more
    public TestDynamicBufferDiscard()
    {
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
    }

    // One triangle that covers the whole target
    private static readonly Vector2[] Positions = [new(-1, -1), new(-1, 3), new(3, -1)];

    [Fact]
    public void DiscardKeepsTheContentsOfEarlierDraws()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var targetA = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var targetB = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, targetA);

            colorBuffer.SetData(commandList, new[] { Color.Red });
            drawer.Draw(targetA, positionBuffer, colorBuffer, instance: 0);

            colorBuffer.SetData(commandList, new[] { Color.Blue });
            drawer.Draw(targetB, positionBuffer, colorBuffer, instance: 0);

            Assert.Equal(Color.Red, ReadColor(commandList, targetA));
            Assert.Equal(Color.Blue, ReadColor(commandList, targetB));
        });
    }

    [Fact]
    public unsafe void NoOverwriteAfterDiscardWritesTheSameBuffer()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var targetA = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var targetB = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, targetA);

            // Discard and write the first instance, then append the second one without discarding
            var discarded = commandList.MapSubResource(colorBuffer, 0, MapMode.WriteDiscard);
            ((Color*) discarded.DataBox.DataPointer)[0] = Color.Red;
            commandList.UnmapSubResource(discarded);

            var appended = commandList.MapSubResource(colorBuffer, 0, MapMode.WriteNoOverwrite);
            ((Color*) appended.DataBox.DataPointer)[1] = Color.Lime;
            commandList.UnmapSubResource(appended);

            drawer.Draw(targetA, positionBuffer, colorBuffer, instance: 0);
            drawer.Draw(targetB, positionBuffer, colorBuffer, instance: 1);

            Assert.Equal(Color.Red, ReadColor(commandList, targetA));
            Assert.Equal(Color.Lime, ReadColor(commandList, targetB));
        });
    }

    [Fact]
    public void DiscardAfterBindingChangesWhatLaterDrawsRead()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, target);

            colorBuffer.SetData(commandList, new[] { Color.Red });
            drawer.Bind(target, positionBuffer, colorBuffer);

            // The buffer is bound already: the draw reads what the discard wrote
            colorBuffer.SetData(commandList, new[] { Color.Blue });
            drawer.DrawBound(instance: 0);

            Assert.Equal(Color.Blue, ReadColor(commandList, target));
        });
    }

    [SkippableFact]
    public void DiscardAfterBindingAVertexBufferInAHighSlotChangesWhatLaterDrawsRead()
    {
        const int HighSlot = 20;

        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, target, HighSlot);

            colorBuffer.SetData(commandList, new[] { Color.Red });
            drawer.Bind(target, positionBuffer, colorBuffer);

            colorBuffer.SetData(commandList, new[] { Color.Blue });
            drawer.DrawBound(instance: 0);

            Assert.Equal(Color.Blue, ReadColor(commandList, target));
        });
    }

    [SkippableFact]
    public void DiscardOnAClosedCommandListDoesNotRebind()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Vulkan or GraphicsPlatform.Direct3D12, "Only these track the bound buffers.");

            using var vertexBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);
            using var indexBuffer = Buffer.Index.New(device, 4 * sizeof(ushort), GraphicsResourceUsage.Dynamic);
            using var commandList = CommandList.New(device);

            commandList.SetVertexBuffer(0, vertexBuffer, 0, 4);
            commandList.SetIndexBuffer(indexBuffer, 0, false);
            device.ExecuteCommandList(commandList.Close());

            // Between Close and Reset there is no native command list to record the binding in
            vertexBuffer.SetData(commandList, new[] { Color.Red });
            indexBuffer.SetData(commandList, new ushort[] { 0, 1, 2 });

            commandList.Reset();
            vertexBuffer.SetData(commandList, new[] { Color.Blue });
            device.ExecuteCommandList(commandList.Close());
        });
    }

    [Fact]
    public void DiscardAfterBindingAnIndexBufferChangesWhatLaterDrawsRead()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, [Color.Lime]);
            using var indexBuffer = Buffer.Index.New(device, 4 * sizeof(ushort), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, target);

            // Degenerate triangle: draws nothing
            indexBuffer.SetData(commandList, new ushort[] { 0, 0, 0 });
            drawer.Bind(target, positionBuffer, colorBuffer);
            commandList.SetIndexBuffer(indexBuffer, 0, false);

            indexBuffer.SetData(commandList, new ushort[] { 0, 1, 2 });
            drawer.DrawBoundIndexed();

            Assert.Equal(Color.Lime, ReadColor(commandList, target));
        });
    }

#if STRIDE_GRAPHICS_API_VULKAN
    [Fact]
    public void RepeatedDiscardsReuseNativeAllocations()
    {
        const int DiscardsPerFrame = 3;
        const int FrameCount = 50;

        var game = (GameTestBase) Activator.CreateInstance(GetType());
        game.FrameGameSystem.IsUnitTestFeeding = true;

        Buffer buffer = null;
        game.FrameGameSystem.Draw(0, () => buffer = Buffer.Vertex.New(game.GraphicsDevice, 16 * 1024, GraphicsResourceUsage.Dynamic));

        for (var frame = 1; frame <= FrameCount; frame++)
        {
            game.FrameGameSystem.Draw(frame, () =>
            {
                var commandList = game.GraphicsContext.CommandList;
                for (var i = 0; i < DiscardsPerFrame; i++)
                    buffer.SetData(commandList, new[] { Color.Red });
            });
        }

        game.FrameGameSystem.Draw(FrameCount + 1, () =>
        {
            Assert.InRange(buffer.NativeAllocationCount, 1, DiscardsPerFrame * (GraphicsDevice.MaxFramesInFlight + 1) + 1);
            buffer.Dispose();
        });

        RunGameTest(game);
    }

    [Fact]
    public void IdleBufferReleasesItsRetiredAllocations()
    {
        const int BurstSize = 20;
        const int IdleFrameCount = 20;
        const int BufferSize = 16 * 1024;

        var game = (GameTestBase) Activator.CreateInstance(GetType());
        game.FrameGameSystem.IsUnitTestFeeding = true;

        Buffer buffer = null;
        long memoryBefore = 0;
        game.FrameGameSystem.Draw(0, () =>
        {
            memoryBefore = game.GraphicsDevice.BuffersMemory;
            buffer = Buffer.Vertex.New(game.GraphicsDevice, BufferSize, GraphicsResourceUsage.Dynamic);
        });

        game.FrameGameSystem.Draw(1, () =>
        {
            var commandList = game.GraphicsContext.CommandList;
            for (var i = 0; i < BurstSize; i++)
                buffer.SetData(commandList, new[] { Color.Red });

            Assert.Equal(BurstSize, buffer.RetiredAllocationCount);
            Assert.Equal(memoryBefore + BufferSize * (BurstSize + 1), game.GraphicsDevice.BuffersMemory);
        });

        game.FrameGameSystem.Draw(1 + IdleFrameCount, () =>
        {
            Assert.Equal(0, buffer.RetiredAllocationCount);
            Assert.Equal(memoryBefore + BufferSize, game.GraphicsDevice.BuffersMemory);
            buffer.Dispose();
        });

        RunGameTest(game);
    }
#endif

    [SkippableFact]
    public void DiscardAfterSettingDescriptorsChangesWhatLaterDrawsRead()
    {
        SkipTestForGraphicPlatform(GraphicsPlatform.Direct3D12, "A Descriptor Set copies the view of a Buffer when it is filled, and a discard creates a new resource: the set keeps the view of the old one");

        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var colorBuffer = Buffer.Constant.New(device, 4 * sizeof(float), GraphicsResourceUsage.Dynamic);

            using var effect = new EffectInstance(game.EffectSystem.LoadEffect("SimpleEffect").WaitForResult());
            effect.UpdateEffect(device);

            var layouts = effect.DescriptorReflection.Layouts.Select(layout => layout.Layout).ToArray();
            var typeCounts = new Dictionary<EffectParameterClass, int>();
            foreach (var layout in layouts)
            {
                foreach (var entry in layout.Entries)
                {
                    typeCounts.TryGetValue(entry.Class, out var count);
                    typeCounts[entry.Class] = count + 1;
                }
            }

            var poolTypes = typeCounts.Select(pair => new DescriptorTypeCount(pair.Key, pair.Value)).ToArray();
            using var pool = DescriptorPool.New(device, poolTypes);
            var descriptorSets = new DescriptorSet[layouts.Length];
            for (var i = 0; i < layouts.Length; i++)
            {
                descriptorSets[i] = DescriptorSet.New(device, pool, DescriptorSetLayout.New(device, layouts[i]));
                for (var slot = 0; slot < layouts[i].Entries.Count; slot++)
                {
                    if (layouts[i].Entries[slot].Class == EffectParameterClass.ConstantBuffer)
                        descriptorSets[i].SetConstantBuffer(slot, colorBuffer, 0, colorBuffer.SizeInBytes);
                }
            }

            var pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();
            pipelineState.State.RootSignature = effect.RootSignature;
            pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
            pipelineState.State.InputElements = PrimitiveQuad.VertexDeclaration.CreateInputElements();
            pipelineState.State.PrimitiveType = PrimitiveQuad.PrimitiveType;

            colorBuffer.SetData(commandList, new[] { new Color4(1, 0, 0, 1) });

            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();
            commandList.Clear(target, Color.Black);
            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetDescriptorSets(0, descriptorSets);

            // The descriptor set is set already: the draw reads what the discard wrote
            colorBuffer.SetData(commandList, new[] { new Color4(0, 0, 1, 1) });
            device.PrimitiveQuad.Draw(commandList);

            Assert.Equal(Color.Blue, ReadColor(commandList, target));
        });
    }

    private static Color ReadColor(CommandList commandList, Texture target)
    {
        var pixels = target.GetData<Color>(commandList);
        foreach (var pixel in pixels)
        {
            Assert.Equal(pixels[0], pixel);
        }
        return pixels[0];
    }

    /// <summary>
    ///   Draws one triangle that fills the target with the color of an instance read from a per-instance vertex stream.
    /// </summary>
    private sealed class Drawer : IDisposable
    {
        private readonly GameTestBase game;
        private readonly int colorSlot;
        private readonly MutablePipelineState pipelineState;
        private readonly EffectInstance effect;

        public Drawer(GameTestBase game, Texture target, int colorSlot = 1)
        {
            this.game = game;
            this.colorSlot = colorSlot;
            var device = game.GraphicsDevice;

            pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();
            pipelineState.State.RasterizerState = RasterizerStates.CullNone;
            pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
            // Vulkan needs the slots to be consecutive: the unused ones below the color slot are filled with elements the shader does not read
            var inputElements = new List<InputElementDescription>
            {
                new() { SemanticName = "POSITION", Format = PixelFormat.R32G32_Float, InputSlot = 0 },
            };
            for (var slot = 1; slot < colorSlot; slot++)
            {
                inputElements.Add(new InputElementDescription { SemanticName = "TEXCOORD", SemanticIndex = slot, Format = PixelFormat.R32G32_Float, InputSlot = slot });
            }
            inputElements.Add(new InputElementDescription
            {
                SemanticName = "COLOR",
                Format = PixelFormat.R8G8B8A8_UNorm,
                InputSlot = colorSlot,
                InputSlotClass = InputClassification.Instance,
                InstanceDataStepRate = 1,
            });
            pipelineState.State.InputElements = inputElements.ToArray();

            effect = new EffectInstance(game.EffectSystem.LoadEffect("InstancedDrawTestShader").WaitForResult());
            effect.UpdateEffect(device);
            pipelineState.State.RootSignature = effect.RootSignature;
            pipelineState.State.EffectBytecode = effect.Effect.Bytecode;

            var commandList = game.GraphicsContext.CommandList;
            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();
        }

        public void Draw(Texture target, Buffer positionBuffer, Buffer colorBuffer, int instance)
        {
            Bind(target, positionBuffer, colorBuffer);
            DrawBound(instance);
        }

        public void Bind(Texture target, Buffer positionBuffer, Buffer colorBuffer)
        {
            var commandList = game.GraphicsContext.CommandList;

            commandList.Clear(target, Color.Black);
            commandList.SetRenderTargetAndViewport(null, target);
            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetVertexBuffer(0, positionBuffer, 0, 8);
            for (var slot = 1; slot < colorSlot; slot++)
                commandList.SetVertexBuffer(slot, positionBuffer, 0, 8);
            commandList.SetVertexBuffer(colorSlot, colorBuffer, 0, 4);
        }

        public void DrawBound(int instance)
        {
            var commandList = game.GraphicsContext.CommandList;

            effect.Apply(game.GraphicsContext);

            commandList.DrawInstanced(3, 1, startInstanceLocation: instance);
        }

        public void DrawBoundIndexed()
        {
            effect.Apply(game.GraphicsContext);

            game.GraphicsContext.CommandList.DrawIndexed(3);
        }

        public void Dispose()
        {
            effect.Dispose();
        }
    }
}
