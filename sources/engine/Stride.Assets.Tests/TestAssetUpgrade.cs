// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Reflection;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Opens asset files saved by released versions as a project upgrade does (migration, then load), saves them again
    /// and compares the result with the expected file next to them, or with the file when it has none. See AssetUpgrade/README.md.
    /// </summary>
    public class TestAssetUpgrade
    {
        private const string UpdateVariable = "STRIDE_ASSET_UPGRADE_UPDATE";

        private static readonly string FixturesDirectory = Path.Combine(AppContext.BaseDirectory, "AssetUpgrade", "Fixtures");

        static TestAssetUpgrade()
        {
            // Registers what nothing else here loads, as the editor does with the packages of a project: the particle types
            // (components, the render feature of compositors) and the asset types of the physics plugins, with their upgraders
            RuntimeHelpers.RunModuleConstructor(typeof(Stride.Particles.Components.ParticleSystemComponent).Module.ModuleHandle);
            AssemblyRegistry.Register(typeof(Stride.Physics.Assets.ColliderShapeAsset).Assembly, AssemblyCommonCategories.Assets);
            AssemblyRegistry.Register(typeof(Stride.BepuPhysics.Assets.HullAsset).Assembly, AssemblyCommonCategories.Assets);
        }

        public static IEnumerable<object[]> Fixtures()
            => Directory.EnumerateFiles(FixturesDirectory).Order(StringComparer.Ordinal).Select(path => new object[] { Path.GetFileName(path) });

        [Theory]
        [MemberData(nameof(Fixtures))]
        public void UpgradesAssetOfARelease(string fixture)
        {
            var path = Path.Combine(FixturesDirectory, fixture);
            // The warnings of the migration are what upgraders tell the user (a V-HACD upgrader says its output may
            // differ); those of the load are what didn't load
            var migrationLogger = new LoggerResult();
            var logger = new LoggerResult();

            var file = new PackageLoadingAssetFile(path, FixturesDirectory);
            var context = new AssetMigrationContext(null, file.ToReference(), file.FilePath.ToOSPath(), migrationLogger);
            AssetMigration.MigrateAssetIfNeeded(context, file);
            // A package is no asset, so the load asks for any type
            var loaded = file.AssetContent is { } migrated
                ? AssetFileSerializer.Load<object>(new MemoryStream(migrated), path, logger)
                : AssetFileSerializer.Load<object>(path, logger);

            ConvertPaths(loaded.Asset, fixture, UPathType.Absolute, logger);
            ConvertPaths(loaded.Asset, fixture, UPathType.Relative, logger);
            using var stream = new MemoryStream();
            AssetFileSerializer.Save(stream, loaded.Asset, loaded.YamlMetadata, logger);
            var upgraded = Normalize(Encoding.UTF8.GetString(stream.ToArray()));

            foreach (var message in migrationLogger.Messages.Concat(logger.Messages))
                Console.WriteLine(message);
            Assert.False(migrationLogger.HasErrors || logger.HasErrors, $"{fixture}: the upgrade logged errors");
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
                var sourcePath = Path.Combine(SourceDirectory(), "AssetUpgrade", "Expected", fixture);
                if (changed)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
                    File.WriteAllText(sourcePath, upgraded);
                    Console.WriteLine($"Expected file updated: {sourcePath}");
                }
                else if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                    Console.WriteLine($"Expected file removed, the upgrade no longer changes the file: {sourcePath}");
                }
                return;
            }

            var hasExpected = File.Exists(expectedPath);
            var expected = hasExpected ? Normalize(File.ReadAllText(expectedPath)) : original;
            if (upgraded != expected)
            {
                var actualPath = Path.Combine(Path.GetTempPath(), "stride-asset-upgrade", fixture);
                Directory.CreateDirectory(Path.GetDirectoryName(actualPath));
                File.WriteAllText(actualPath, upgraded);
                var what = hasExpected
                    ? (changed ? $"differs from AssetUpgrade/Expected/{fixture}" : $"is the file as it was, not AssetUpgrade/Expected/{fixture}")
                    : "differs from the file, which has no expected file";
                Assert.Fail($"{fixture}: the upgraded file {what} at line {FirstDifferentLine(upgraded, expected)}. "
                    + $"It is written to {actualPath}; if the change is expected, run the test with {UpdateVariable}=1.");
            }
        }

        // As a session does after the load and before the save: the paths of an asset, its source hashes among them, are
        // absolute while it is loaded
        private static void ConvertPaths(object loaded, string fixture, UPathType convertTo, ILogger logger)
        {
            if (loaded is not Asset asset)
                return;
            var package = new Package { FullPath = Path.Combine(FixturesDirectory, "Fixtures.sdpkg") };
            var item = new AssetItem(Path.GetFileNameWithoutExtension(fixture), asset) { SourceFolder = UDirectory.This };
            package.Assets.Add(item);
            AssetAnalysis.Run(item, logger, new AssetAnalysisParameters { IsProcessingUPaths = true, ConvertUPathTo = convertTo });
            package.Assets.Remove(item);
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
