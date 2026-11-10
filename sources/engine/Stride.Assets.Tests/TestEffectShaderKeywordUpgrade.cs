// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.4.2 project upgrade: an .sdfx of Stride 4.3 and older that declares an effect with <c>shader</c> declares it with
    /// <c>effect</c>.
    /// </summary>
    public class TestEffectShaderKeywordUpgrade : IDisposable
    {
        private const string OldEffect = "namespace Game\r\n{\r\n    shader Main\r\n    {\r\n        mixin Base;\r\n    };\r\n}\r\n";

        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-effect-upgrade-{Guid.NewGuid():N}");

        public TestEffectShaderKeywordUpgrade()
        {
            Directory.CreateDirectory(Path.Combine(directory, "Effects"));
            Directory.CreateDirectory(Path.Combine(directory, "obj", "Debug"));
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        // Only the .sdfx files of the project change, out of its build output, with their byte order mark
        [Fact]
        public void UpgradesTheEffectFilesOfTheProject()
        {
            File.WriteAllText(Path.Combine(directory, "Effects", "WithBom.sdfx"), OldEffect, new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(directory, "Effects", "WithoutBom.sdfx"), OldEffect, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(directory, "Effects", "Main.sdsl"), OldEffect);
            File.WriteAllText(Path.Combine(directory, "obj", "Debug", "Copy.sdfx"), OldEffect);
            var log = new LoggerResult();

            StridePackageUpgrader.UpgradeEffectShaderKeywords(new ProjectUpgradeContext(null!, null!, new UFile(Path.Combine(directory, "Game.csproj")), log));

            var upgraded = OldEffect.Replace("shader Main", "effect Main");
            Assert.Equal([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(upgraded)], File.ReadAllBytes(Path.Combine(directory, "Effects", "WithBom.sdfx")));
            Assert.Equal(Encoding.UTF8.GetBytes(upgraded), File.ReadAllBytes(Path.Combine(directory, "Effects", "WithoutBom.sdfx")));
            Assert.Equal(OldEffect, File.ReadAllText(Path.Combine(directory, "Effects", "Main.sdsl")));
            Assert.Equal(OldEffect, File.ReadAllText(Path.Combine(directory, "obj", "Debug", "Copy.sdfx")));
            Assert.Equal(2, log.Messages.Count(x => x.Type == LogMessageType.Info && x.Text.Contains("declared with 'effect'", StringComparison.Ordinal)));
        }

        [Fact]
        public void WritesEffectInPlaceOfTheShaderKeyword()
        {
            var upgraded = StridePackageUpgrader.UpgradeEffectShaderKeywords("""
                // A shader that declares an effect
                namespace Game.shaders
                {
                    shader Main
                    {
                        mixin shaderBase;
                        mixin child Child;
                        mixin compose shaderSlot = "shader";
                    };

                    partial  shader /* shader */ Child { mixin Other; }
                }

                shader Root { mixin Base; }
                """, out var count);

            Assert.Equal("""
                // A shader that declares an effect
                namespace Game.shaders
                {
                    effect Main
                    {
                        mixin shaderBase;
                        mixin child Child;
                        mixin compose shaderSlot = "shader";
                    };

                    partial  effect /* shader */ Child { mixin Other; }
                }

                effect Root { mixin Base; }
                """, upgraded);
            Assert.Equal(3, count);
        }

        [Theory]
        // Already upgraded
        [InlineData("namespace Game\r\n{\r\n    effect Main\r\n    {\r\n        mixin Base;\r\n    };\r\n}\r\n")]
        // A shader class, whose fix is to move it to an .sdsl
        [InlineData("shader S { float4 F() { return 0; } };")]
        // A file that doesn't parse: its build reports the error
        [InlineData("shader Main { mixin Base; } }")]
        // 'shader' only in a comment, a string or a name
        [InlineData("// shader Main { }\neffect shaderMain { mixin shaderBase; mixin compose x = \"shader\"; }")]
        public void LeavesTheOtherFilesAlone(string text)
        {
            Assert.Null(StridePackageUpgrader.UpgradeEffectShaderKeywords(text, out var count));
            Assert.Equal(0, count);
        }
    }
}
