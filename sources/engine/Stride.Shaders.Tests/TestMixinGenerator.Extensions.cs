// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;

using Xunit;

namespace Stride.Shaders.Tests
{
    /// <summary>
    /// Extension methods used by <see cref="TestMixinGenerator"/>
    /// </summary>
    static class TestMixinGeneratorExtensions
    {
        /// <summary>
        /// Checks a mixin contains the specific names.
        /// </summary>
        /// <param name="mixin">The mixin.</param>
        /// <param name="names">The names to check.</param>
        public static void CheckMixin(this ShaderMixinSource mixin, params string[] names)
        {
            var expectingNames = string.Join(",", names);
            var resultNames = string.Join(",", mixin.Mixins.Select(source => source.ToString()));

            var messageMixins = string.Format("Invalid result for mixin: [{0}] Expecting [{1}]", resultNames, expectingNames);

            Assert.True(resultNames == expectingNames, messageMixins);
        }

        /// <summary>
        /// Checks the composition is declared in the mixin.
        /// </summary>
        /// <param name="mixin">The mixin.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void CheckComposition(this ShaderMixinSource mixin, string key, string value)
        {
            ShaderSource source;
            Assert.True(mixin.Compositions.TryGetValue(key, out source), $"Unable to find key [{key}] in mixin compositions");

            Assert.True(source != null, $"Source composition for key [{key}] cannot be null");

            var classSource = source as ShaderClassCode;
            if (classSource != null)
            {
                var sourceString = classSource.ToClassName();
                Assert.True(sourceString == value, $"Invalid composition for key [{key}]: [{sourceString}] expecting [{value}]");
            }
            else
            {
                var mixinSource = source as ShaderMixinSource;
                if (mixinSource != null)
                {
                    mixinSource.CheckMixin(value);
                }
            }
        }

        /// <summary>
        /// Checks the macro is declared in the mixin
        /// </summary>
        /// <param name="mixin">The mixin.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void CheckMacro(this ShaderMixinSource mixin, string key, object value)
        {
            var macro = mixin.Macros.FirstOrDefault(tuple => tuple.Name == key);
            Assert.True(macro.Name != null, $"Invalid macro [{key}] cannot be null");

            var macroValue = macro.Definition.ToString();
            Assert.True(value.ToString() == macroValue, $"Invalid macro [{key}] value [{macroValue}] != Expecting [{value}]");
        }
    }
}
