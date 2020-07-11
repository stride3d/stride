// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using Stride.Rendering.Images;
using Stride.Games;
using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// The the pass 2 of lambertian prefiltering SH
    /// </summary>
    public class TestLambertPrefilteringSHPass2 : GraphicTestGameBase
    {
        private const int Order = 2;

        private const int NbOfCoeffs = Order * Order;

        private const int NbOfSums = 8;

        private Int2 nbOfGroups = new Int2(2, 3);

        private Vector4[] inputBufferData;

        private ComputeEffectShader pass2;

        private Buffer outputBuffer;

        private Buffer<Vector4> inputBuffer;

        private readonly bool assertResults;

        private readonly Int2 screenSize = new Int2(1200, 900);

        public TestLambertPrefilteringSHPass2() : this(true)
        {
        }

        protected TestLambertPrefilteringSHPass2(bool assertResults)
        {
            this.assertResults = assertResults;
            GraphicsDeviceManager.PreferredBackBufferWidth = screenSize.X;
            GraphicsDeviceManager.PreferredBackBufferHeight = screenSize.Y;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            CreateBufferData();

            inputBuffer = Buffer.Typed.New(GraphicsDevice, inputBufferData, PixelFormat.R32G32B32A32_Float, true);
            outputBuffer = Buffer.Typed.New(GraphicsDevice, NbOfCoeffs * nbOfGroups.X * nbOfGroups.Y, PixelFormat.R32G32B32A32_Float, true);

            var context = RenderContext.GetShared(Services);
            pass2 = new ComputeEffectShader(context) { ShaderSourceName = "LambertianPrefilteringSHEffectPass2", };
        }

        protected override void Draw(GameTime gameTime)
        {
            var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

            base.Draw(gameTime);

            pass2.ThreadNumbers = new Int3(NbOfSums, 1, 1);
            pass2.ThreadGroupCounts = new Int3(nbOfGroups.X, nbOfGroups.Y, NbOfCoeffs);
            pass2.Parameters.Set(LambertianPrefilteringSHParameters.BlockSize, NbOfSums);
            pass2.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, Order);
            pass2.Parameters.Set(LambertianPrefilteringSHPass2Keys.InputBuffer, inputBuffer);
            pass2.Parameters.Set(LambertianPrefilteringSHPass2Keys.OutputBuffer, outputBuffer);
            pass2.Draw(renderDrawContext);

            // Get the data out of the final buffer
            var finalsValues = outputBuffer.GetData<Vector4>(GraphicsContext.CommandList);

            // performs last possible additions, normalize the result and store it in the SH
            var result = new Vector4[NbOfCoeffs];
            for (var c = 0; c < NbOfCoeffs; c++)
            {
                var coeff = Vector4.Zero;
                for (var f = 0; f < nbOfGroups.X * nbOfGroups.Y; ++f)
                {
                    coeff += finalsValues[NbOfCoeffs * f + c];
                }
                result[c] = coeff;
            }

            var nbOfTerms = NbOfSums * nbOfGroups.X * nbOfGroups.Y;
            var valueSum = (nbOfTerms - 1) * nbOfTerms / 2;

            if (assertResults)
            {
                Assert.Equal(new Vector4(valueSum, 0, 0, 0), result[0]);
                Assert.Equal(new Vector4(0, 2 * valueSum, 0, 0), result[1]);
                Assert.Equal(new Vector4(0, 0, 3 * valueSum, 0), result[2]);
                Assert.Equal(new Vector4(0, 0, 0, 4 * valueSum), result[3]);
            }
        }

        private void CreateBufferData()
        {
            inputBufferData = new Vector4[NbOfCoeffs * NbOfSums * nbOfGroups.X * nbOfGroups.Y];

            // initialize values
            for (int i = 0; i < NbOfCoeffs; i++)
            {
                for (int u = 0; u < NbOfSums * nbOfGroups.X * nbOfGroups.Y; ++u)
                {
                    var value = Vector4.Zero;
                    value[i] = (1+i) * u;
                    inputBufferData[i + u * NbOfCoeffs] = value;
                }
            }
        }

        [SkippableFact]
        public void RunTestPass2()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);
            IgnoreGraphicPlatform(GraphicsPlatform.Vulkan);

            RunGameTest(new TestLambertPrefilteringSHPass2());
        }
    }
}
