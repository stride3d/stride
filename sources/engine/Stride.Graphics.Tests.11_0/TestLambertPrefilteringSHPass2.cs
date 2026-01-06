// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using Stride.Rendering.Images;
using Stride.Games;

namespace Stride.Graphics.Tests
{
    /// <summary>
    ///   Test game that tests the 2nd pass of the Lambertian pre-filtering Spherical Harmonics algorithm.
    /// </summary>
    public class TestLambertPrefilteringSHPass2 : GraphicTestGameBase
    {
        private const int Order = 2;
        private const int NumberOfCoefficients = Order * Order;
        private const int NumberOfSums = 8;

        private static readonly Int2 GroupCount = new(2, 3);
        private static readonly Int2 ScreenSize = new(1200, 900);

        private ComputeEffectShader computeShader;

        private Buffer outputBuffer;
        private Vector4[] inputBufferData;
        private Buffer<Vector4> inputBuffer;

        private readonly bool assertResults;


        /// <summary>
        ///   Initializes a new instance of the <see cref="TestLambertPrefilteringSHPass2"/> class.
        /// </summary>
        public TestLambertPrefilteringSHPass2() : this(assertResults: true) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="TestLambertPrefilteringSHPass2"/> class.
        /// </summary>
        /// <param name="assertResults">A value indicating if the test should assert the computed results.</param>
        protected TestLambertPrefilteringSHPass2(bool assertResults)
        {
            this.assertResults = assertResults;
            GraphicsDeviceManager.PreferredBackBufferWidth = ScreenSize.X;
            GraphicsDeviceManager.PreferredBackBufferHeight = ScreenSize.Y;
        }


        /// <inheritdoc/>
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            CreateBufferData();

            inputBuffer = Buffer.Typed.New(GraphicsDevice, inputBufferData,
                                           elementFormat: PixelFormat.R32G32B32A32_Float,
                                           unorderedAccess: true);

            inputBuffer.Name = "InputBuffer";

            outputBuffer = Buffer.Typed.New(GraphicsDevice,
                                            elementCount: NumberOfCoefficients * GroupCount.X * GroupCount.Y,
                                            elementFormat: PixelFormat.R32G32B32A32_Float,
                                            unorderedAccess: true);

            outputBuffer.Name = "OutputBuffer";

            var context = RenderContext.GetShared(Services);

            computeShader = new ComputeEffectShader(context) { ShaderSourceName = "LambertianPrefilteringSHEffectPass2" };
        }

        /// <inheritdoc/>
        protected override void Draw(GameTime gameTime)
        {
            var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

            base.Draw(gameTime);

            computeShader.ThreadNumbers = new Int3(NumberOfSums, 1, 1);
            computeShader.ThreadGroupCounts = new Int3(GroupCount.X, GroupCount.Y, NumberOfCoefficients);
            computeShader.Parameters.Set(LambertianPrefilteringSHParameters.BlockSize, NumberOfSums);
            computeShader.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, Order);
            computeShader.Parameters.Set(LambertianPrefilteringSHPass2Keys.InputBuffer, inputBuffer);
            computeShader.Parameters.Set(LambertianPrefilteringSHPass2Keys.OutputBuffer, outputBuffer);
            computeShader.Draw(renderDrawContext);

            // Get the data out of the final Buffer
            var finalValues = outputBuffer.GetData<Vector4>(GraphicsContext.CommandList);

            // Performs last possible additions, normalize the result and store it in the SH
            var result = new Vector4[NumberOfCoefficients];
            for (var c = 0; c < NumberOfCoefficients; c++)
            {
                var coeff = Vector4.Zero;
                for (var f = 0; f < GroupCount.X * GroupCount.Y; f++)
                {
                    coeff += finalValues[NumberOfCoefficients * f + c];
                }
                result[c] = coeff;
            }

            var numTerms = NumberOfSums * GroupCount.X * GroupCount.Y;
            var valueSum = (numTerms - 1) * numTerms / 2;

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
            inputBufferData = new Vector4[NumberOfCoefficients * NumberOfSums * GroupCount.X * GroupCount.Y];

            // Initialize values
            for (int i = 0; i < NumberOfCoefficients; i++)
            for (int u = 0; u < NumberOfSums * GroupCount.X * GroupCount.Y; u++)
            {
                var value = Vector4.Zero;
                value[i] = (1 + i) * u;
                inputBufferData[i + u * NumberOfCoefficients] = value;
            }
        }


        [SkippableFact]
        public void RunTestPass2()
        {
            SkipTestForGraphicPlatform(GraphicsPlatform.OpenGLES);
            SkipTestForGraphicPlatform(GraphicsPlatform.Vulkan);

            RunGameTest(new TestLambertPrefilteringSHPass2());
        }
    }
}
