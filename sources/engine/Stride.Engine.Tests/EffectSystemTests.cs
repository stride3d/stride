// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// The requests of one permutation give one <see cref="Effect"/>, so the draws that use it share one pipeline state.
    /// </summary>
    public class EffectSystemTests : GameTestBase
    {
        [Fact]
        public void OnePermutationGivesOneEffect()
        {
            PerformTest(game =>
            {
                // A shader class, as two DynamicEffectInstance("BackgroundShader") load it: the early cache of the effect
                // system keeps only .sdfx effects, so each request goes to the compiler cache, the second after the first compiled
                var first = game.EffectSystem.LoadEffect("BackgroundShader", new CompilerParameters()).WaitForResult();
                var second = game.EffectSystem.LoadEffect("BackgroundShader", new CompilerParameters()).WaitForResult();

                Assert.Same(first.Bytecode, second.Bytecode);
                Assert.Same(first, second);
            });
        }
    }
}
