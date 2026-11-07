// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Stride.Assets.Entities;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Opens scenes saved by released versions as a project upgrade does (migration, then load), saves them again and
    /// compares the result with the expected file next to them, or with the scene when it has none. See SceneUpgrade/README.md.
    /// </summary>
    public class TestSceneUpgrader
    {
        private const string UpdateVariable = "STRIDE_SCENE_UPGRADE_UPDATE";

        private static readonly string FixturesDirectory = Path.Combine(AppContext.BaseDirectory, "SceneUpgrade", "Fixtures");

        public static IEnumerable<object[]> Fixtures()
            => Directory.EnumerateFiles(FixturesDirectory, "*.sdscene").Order(StringComparer.Ordinal).Select(path => new object[] { Path.GetFileName(path) });

        [Theory]
        [MemberData(nameof(Fixtures))]
        public void UpgradesSceneOfARelease(string fixture)
        {
            var path = Path.Combine(FixturesDirectory, fixture);
            var logger = new LoggerResult();

            var file = new PackageLoadingAssetFile(path, FixturesDirectory);
            var context = new AssetMigrationContext(null, file.ToReference(), file.FilePath.ToOSPath(), logger);
            AssetMigration.MigrateAssetIfNeeded(context, file);
            var loaded = file.AssetContent is { } migrated
                ? AssetFileSerializer.Load<SceneAsset>(new MemoryStream(migrated), path, logger)
                : AssetFileSerializer.Load<SceneAsset>(path, logger);

            using var stream = new MemoryStream();
            AssetFileSerializer.Save(stream, loaded.Asset, loaded.YamlMetadata, logger);
            var upgraded = Normalize(Encoding.UTF8.GetString(stream.ToArray()));

            foreach (var message in logger.Messages)
                Console.WriteLine(message);
            Assert.False(logger.HasErrors, $"{fixture}: the upgrade logged errors");
            // The scripts of the sample can't load, its game assembly isn't here; every engine type must
            var notLoaded = logger.Messages
                .Where(message => message.Type == LogMessageType.Warning && !IsScriptOfTheSample(message.ToString()))
                .Select(message => message.Text)
                .ToList();
            Assert.True(notLoaded.Count == 0, $"{fixture}: {string.Join(Environment.NewLine, notLoaded)}");
            // A fixture the upgrade changes has the result in Expected/; one it leaves as it is has no file there, and must
            // stay as it is: a member saved by today's types and not by its release shows there
            var original = Normalize(File.ReadAllText(path));
            var changed = upgraded != original;
            var expectedPath = Path.Combine(FixturesDirectory, "..", "Expected", fixture);
            if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
            {
                var sourcePath = Path.Combine(SourceDirectory(), "SceneUpgrade", "Expected", fixture);
                if (changed)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
                    File.WriteAllText(sourcePath, upgraded);
                    Console.WriteLine($"Expected file updated: {sourcePath}");
                }
                else if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                    Console.WriteLine($"Expected file removed, the upgrade no longer changes the scene: {sourcePath}");
                }
                return;
            }

            var hasExpected = File.Exists(expectedPath);
            var expected = hasExpected ? Normalize(File.ReadAllText(expectedPath)) : original;
            if (upgraded != expected)
            {
                var actualPath = Path.Combine(Path.GetTempPath(), "stride-scene-upgrade", fixture);
                Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
                File.WriteAllText(actualPath, upgraded);
                var what = hasExpected
                    ? (changed ? $"differs from SceneUpgrade/Expected/{fixture}" : $"is the scene as it was, not SceneUpgrade/Expected/{fixture}")
                    : "differs from the scene, which has no expected file";
                Assert.Fail($"{fixture}: the upgraded scene {what} at line {FirstDifferentLine(upgraded, expected)}. "
                    + $"It is written to {actualPath}; if the change is expected, run the test with {UpdateVariable}=1.");
            }
        }

        // The exception of the warning names the tag the load can't resolve: [!Gameplay.SceneStreaming,TopDownRPG.Game]
        private static bool IsScriptOfTheSample(string warning)
            => Regex.Match(warning, @"Unable to resolve tag \[![^,\]]+,(?<assembly>[^\]]+)\]") is { Success: true } match
                && !match.Groups["assembly"].Value.StartsWith("Stride", StringComparison.Ordinal);

        private static string SourceDirectory([CallerFilePath] string sourceFile = "") => Path.GetDirectoryName(sourceFile);

        // The fixtures are checked out with the line endings of the OS
        private static string Normalize(string text) => text.Replace("\r\n", "\n");

        private static int FirstDifferentLine(string actual, string expected)
        {
            var actualLines = actual.Split('\n');
            var expectedLines = expected.Split('\n');
            var line = 0;
            while (line < actualLines.Length && line < expectedLines.Length && actualLines[line] == expectedLines[line])
                line++;
            return line + 1;
        }
    }
}
