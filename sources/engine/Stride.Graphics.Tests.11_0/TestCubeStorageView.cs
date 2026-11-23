// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

public class TestCubeStorageView : GraphicTestGameBase
{
    private const int FaceSize = 4;

    private ComputeEffectShader computeEffect;
    private Texture cube;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ComputeShaderWritesAllCubeFaces);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        cube = Texture.NewCube(GraphicsDevice, FaceSize, PixelFormat.R32G32B32A32_Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess).DisposeBy(this);

        computeEffect = new ComputeEffectShader(RenderContext.GetShared(Services))
        {
            ShaderSourceName = "CubeStorageTestShader",
            ThreadNumbers = new Int3(FaceSize, FaceSize, 1),
            ThreadGroupCounts = new Int3(1, 1, 6),
        };
        computeEffect.DisposeBy(this);
    }

    private void ComputeShaderWritesAllCubeFaces()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        commandList.ResourceBarrierTransition(cube, BarrierLayout.UnorderedAccess);

        computeEffect.Parameters.Set(CubeStorageTestShaderKeys.Output, cube);
        ((RendererBase)computeEffect).Draw(renderDrawContext);

        for (int face = 0; face < 6; face++)
        {
            var pixels = cube.GetData<Vector4>(commandList, face, 0);

            Assert.Equal(FaceSize * FaceSize, pixels.Length);
            for (int y = 0; y < FaceSize; y++)
            {
                for (int x = 0; x < FaceSize; x++)
                {
                    Assert.Equal(new Vector4(x, y, face, 1), pixels[(y * FaceSize) + x]);
                }
            }
        }
    }

    [Fact]
    public void ComputeShaderCanWriteAllFacesOfCubeTexture()
    {
        RunGameTest(new TestCubeStorageView());
    }
}
