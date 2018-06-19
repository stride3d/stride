// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;

using NUnit.Framework;

namespace Xenko.Shaders.Tests
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

            Assert.That(resultNames, Is.EqualTo(expectingNames), messageMixins);
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
            Assert.That(mixin.Compositions.TryGetValue(key, out source), Is.True, "Unable to find key [{0}] in mixin compositions", key);

            Assert.That(source, Is.Not.Null, "Source composition for key [{0}] cannot be null", key);

            var classSource = source as ShaderClassSource;
            if (classSource != null)
            {
                var sourceString = classSource.ToClassName();
                Assert.That(sourceString, Is.EqualTo(value), "Invalid composition for key [{0}]: [{1}] expecting [{2}]", key, sourceString, value);
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
            Assert.That(macro, Is.Not.Null, "Invalid macro [{0}] cannot be null", key);

            var macroValue = macro.Definition.ToString();
            Assert.That(macroValue, Is.EqualTo(value.ToString()), "Invalid macro [{0}} value [{1}] != Expecting [{2}]", key, macroValue, value);
        }
    }
}
