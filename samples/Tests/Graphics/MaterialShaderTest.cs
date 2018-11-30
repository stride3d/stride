// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Games.Testing;

namespace Xenko.Samples.Tests
{
    public class MaterialShaderTest : IClassFixture<MaterialShaderTest.Fixture>
    {
        private const string Path = "..\\..\\..\\..\\..\\samplesGenerated\\MaterialShader";

        public class Fixture : SampleTestFixture
        {
            public Fixture() : base(Path, new Guid("f80f8a38-c05a-44bd-ab6d-d2a4f1cf4c58"))
            {
            }
        }

        [Fact]
        public void TestLaunch()
        {
            using (var game = new GameTestingClient(Path, SampleTestsData.TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(2000));
            }
        }

        [Fact]
        public void TestInputs()
        {
            using (var game = new GameTestingClient(Path, SampleTestsData.TestPlatform))
            {
                game.Wait(TimeSpan.FromMilliseconds(2000));

                game.TakeScreenshot();

                game.Wait(TimeSpan.FromMilliseconds(2000));
            }
        }
    }
}
