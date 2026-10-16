// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
/// A texture sampled by a pixel shader, then written by a compute shader in the same frame: what voxel GI does
/// with its voxel textures (sampled by the voxelization pass, then written by the arrangement pass).
/// </summary>
/// <remarks>
///   On Direct3D 11, binding a resource as an unordered access view while it is still bound as a shader
///   resource makes the runtime unbind it, with a debug layer warning (which fails the test). The transition
///   to unordered access has to unbind it first.
/// </remarks>
public class TestUnorderedAccessAfterShaderResource : GraphicTestGameBase
{
    private const int TextureSize = 4;

    private ComputeEffectShader computeEffect;
    private Texture texture;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ComputeShaderWritesTextureSampledBefore);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        texture = Texture.New2D(GraphicsDevice, TextureSize, TextureSize, PixelFormat.R32G32B32A32_Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess).DisposeBy(this);

        computeEffect = new ComputeEffectShader(RenderContext.GetShared(Services))
        {
            ShaderSourceName = "UnorderedAccessOnlyTextureShader",
            ThreadNumbers = new Int3(TextureSize, TextureSize, 1),
            ThreadGroupCounts = new Int3(1, 1, 1),
        };
        computeEffect.DisposeBy(this);
    }

    private void ComputeShaderWritesTextureSampledBefore()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        // Sampled by a pixel shader, which leaves it bound as a shader resource
        commandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
        GraphicsContext.DrawTexture(texture);

        // Then written by a compute shader
        computeEffect.Parameters.Set(UnorderedAccessOnlyTextureShaderKeys.Output, texture);
        ((RendererBase)computeEffect).Draw(renderDrawContext);

        var pixels = texture.GetData<Vector4>(commandList);

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Assert.Equal(new Vector4(x, y, 0, 1), pixels[(y * TextureSize) + x]);
            }
        }
    }

    [Fact]
    public void ComputeShaderCanWriteTextureSampledBefore()
    {
        RunGameTest(new TestUnorderedAccessAfterShaderResource());
    }
}
