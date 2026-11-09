// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Stride.Assets.Effect;
using Stride.Core.Diagnostics;
using Stride.Rendering.Materials;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests that <see cref="EffectLogStore"/> skips and names the entries it can't read, such as an entry with a key of an assembly the project doesn't load.
    /// </summary>
    public class TestEffectLogStore
    {
        private const string Path = @"C:\Game\Assets\EffectCompileLog.sdeffectlog";

        private const string GoodEntry = """
            ---
            !EffectCompileRequest
            EffectName: GoodEffect
            UsedParameters:
                Material.HasNormalMap: true

            """;

        // The shape of a particle entry in a project without Stride.Particles: the second value is a mapping
        private const string UnknownKeysEntry = """
            ---
            !EffectCompileRequest
            EffectName: Stride.Rendering.ParticleEffect
            UsedParameters:
                NotLoaded.UsesSoftEdge: 0
                NotLoaded.BaseColor: !ShaderClassSource
                    ClassName: ComputeColorTextureScaledOffsetDynamicSampler
                    GenericArguments: [NotLoaded.EmissiveMap, TEXCOORD0, Material.Sampler.i0, rgba, Material.TextureScale, Material.TextureOffset]

            """;

        // Read alone, the known key would make another permutation
        private const string OneUnknownKeyEntry = """
            ---
            !EffectCompileRequest
            EffectName: OtherEffect
            UsedParameters:
                Material.HasNormalMap: true
                NotLoaded.Missing: 0

            """;

        static TestEffectLogStore()
        {
            // Registers the keys of Stride.Rendering
            _ = MaterialKeys.HasNormalMap;
        }

        [Fact]
        public void EntryWithUnknownKeysIsSkippedAndNamed()
        {
            var (values, logger) = Load(GoodEntry + UnknownKeysEntry + GoodEntry.Replace("GoodEffect", "GoodEffect2"));

            Assert.Equal(new[] { "GoodEffect", "GoodEffect2" }, values.Select(x => x.EffectName).Order());
            var warning = Assert.Single(logger.Messages);
            Assert.Equal(LogMessageType.Warning, warning.Type);
            Assert.StartsWith($"{Path}(10,", warning.Text);
            Assert.Contains("[Stride.Rendering.ParticleEffect]", warning.Text);
            Assert.Contains("[NotLoaded.UsesSoftEdge]", warning.Text);
        }

        [Fact]
        public void EntryThatWouldLoseAKeyIsSkipped()
        {
            var (values, logger) = Load(OneUnknownKeyEntry);

            Assert.Empty(values);
            var warning = Assert.Single(logger.Messages);
            Assert.StartsWith($"{Path}(6,", warning.Text);
            Assert.Contains("[OtherEffect]", warning.Text);
            Assert.Contains("[NotLoaded.Missing]", warning.Text);
        }

        [Fact]
        public void EntryWithoutParametersIsSkipped()
        {
            var (values, logger) = Load("---\r\n!EffectCompileRequest\r\nEffectName: NoParameters\r\n");

            Assert.Empty(values);
            Assert.Contains("[NoParameters]", Assert.Single(logger.Messages).Text);
        }

        [Fact]
        public void GoodEntriesLoadWithoutWarnings()
        {
            var (values, logger) = Load(GoodEntry);

            var request = Assert.Single(values);
            Assert.Equal(true, request.UsedParameters[MaterialKeys.HasNormalMap]);
            Assert.Empty(logger.Messages);
        }

        private static (Shaders.Compiler.EffectCompileRequest[] Values, LoggerResult Logger) Load(string text)
        {
            var logger = new LoggerResult();
            using var store = new EffectLogStore(new MemoryStream(Encoding.UTF8.GetBytes(text)), logger, Path);
            store.LoadNewValues();
            return (store.GetValues().Select(x => x.Key).ToArray(), logger);
        }
    }
}
