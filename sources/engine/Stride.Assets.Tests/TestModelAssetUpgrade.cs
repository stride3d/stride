// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Tests that models saved by older versions keep their behavior when they are upgraded.
    /// </summary>
    public class TestModelAssetUpgrade
    {
        [Fact]
        public void ModelFromBeforeTangentHandednessKeepsLegacyTangents()
        {
            var upgraded = Upgrade(Model("2.1.0.0"));

            Assert.Contains("LegacyTangentHandedness: true", upgraded);
            Assert.Contains("SerializedVersion: {Stride: 2.2.0.0}", upgraded);
        }

        [Fact]
        public void ModelFromBeforeMorphTargetsKeepsBothBehaviors()
        {
            var upgraded = Upgrade(Model("2.0.0.0"));

            Assert.Contains("ImportMorphTargets: false", upgraded);
            Assert.Contains("LegacyTangentHandedness: true", upgraded);
            Assert.Contains("SerializedVersion: {Stride: 2.2.0.0}", upgraded);
        }

        [Fact]
        public void CurrentModelIsNotUpgraded()
        {
            Assert.Null(Upgrade(Model("2.2.0.0"), expectUpgrade: false));
        }

        private static string Model(string version) =>
            $$"""
            !Model
            Id: 079463bb-d4f9-433d-a5f3-39ba388feaec
            SerializedVersion: {Stride: {{version}}}
            Tags: []
            Source: Model.fbx
            PivotPosition: {X: 0.0, Y: 0.0, Z: 0.0}
            Materials: {}
            Skeleton: null

            """;

        private static string Upgrade(string yaml, bool expectUpgrade = true)
        {
            var directory = Path.Combine(Path.GetTempPath(), "StrideTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "Model.sdm3d");
                File.WriteAllText(path, yaml);

                var logger = new LoggerResult();
                var file = new PackageLoadingAssetFile(path, directory);
                var context = new AssetMigrationContext(null, file.ToReference(), file.FilePath.ToOSPath(), logger);
                var upgraded = AssetMigration.MigrateAssetIfNeeded(context, file, "Stride");
                Assert.False(logger.HasErrors, logger.ToText());
                Assert.Equal(expectUpgrade, upgraded);
                return upgraded ? Encoding.UTF8.GetString(file.AssetContent) : null;
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
