// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Xenko.Core.Assets;
using Xenko.Core.Diagnostics;

namespace Xenko.Assets.Tests
{
    /// <summary>
    /// Test upgrade of scenes
    /// </summary>
    [TestFixture]
    public class TestSceneUpgrader
    {

        /// <summary>
        /// Test upgrade of scene assets from samples. This test makes sense when code has been changed but samples are not yet updated.
        /// </summary>
        [Test]
        public void Test()
        {
            var logger = new LoggerResult();

            var samplesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\samples");
            var files = Directory.EnumerateFiles(samplesPath, "*.xkscene", SearchOption.AllDirectories);

            foreach (var sceneFile in files)
            {
                logger.HasErrors = false;
                logger.Clear();
                Console.WriteLine($"Checking file {sceneFile}");

                var file = new PackageLoadingAssetFile(sceneFile, Path.GetDirectoryName(sceneFile));

                var context = new AssetMigrationContext(null, file.ToReference(), file.FilePath.ToWindowsPath(), logger);
                var needMigration = AssetMigration.MigrateAssetIfNeeded(context, file, "Xenko");

                foreach (var message in logger.Messages)
                {
                    Console.WriteLine(message);
                }

                Assert.False(logger.HasErrors);

                if (needMigration)
                {
                    var result = Encoding.UTF8.GetString(file.AssetContent);
                    Console.WriteLine(result);

                    // We cannot load the Package here, as the package can use code/scripts that are only available when you actually compile project assmeblies
                }
            }
        }
    }
}
