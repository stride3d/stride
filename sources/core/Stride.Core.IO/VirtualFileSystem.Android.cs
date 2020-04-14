// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Compression.Zip;
using Android.Content.PM;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;

namespace Stride.Core.IO
{
    public static partial class VirtualFileSystem
    {
        private const string LastExtractedApkFileName = "LastExtractedAPK";

        public static async Task UnpackAPK()
        {
            // get the apk last update time
            var packageManager = PlatformAndroid.Context.PackageManager;
            var packageInfo = packageManager.GetPackageInfo(PlatformAndroid.Context.PackageName, PackageInfoFlags.Activities);
            var lastUpdateTime = packageInfo.LastUpdateTime;
            var sourceDir = PlatformAndroid.Context.ApplicationInfo.SourceDir;

            // evaluate if asset data should be extracted from apk file
            var shouldExtractAssets = true;
            if (ApplicationTemporary.FileExists(LastExtractedApkFileName))
            {
                Int64 extractedLastUpdateTime = 0;
                using (var file = ApplicationTemporary.OpenStream(LastExtractedApkFileName, VirtualFileMode.Open, VirtualFileAccess.Read))
                {
                    var binaryReader = new BinarySerializationReader(file);
                    binaryReader.Serialize(ref extractedLastUpdateTime, ArchiveMode.Deserialize);
                }

                shouldExtractAssets = extractedLastUpdateTime != lastUpdateTime;
            }

            // Copy assets
            if (shouldExtractAssets)
            {
                var assets = PlatformAndroid.Context.Assets;

                // Make sure assets exists
                var logger = GlobalLogger.GetLogger("VFS");
                CopyFileOrDirectory(logger, sourceDir, "assets/data/", string.Empty);

                // update value of extracted last update time
                using (var stream = ApplicationTemporary.OpenStream(LastExtractedApkFileName, VirtualFileMode.Create, VirtualFileAccess.Write, VirtualFileShare.None))
                {
                    var binaryWriter = new BinarySerializationWriter(stream);
                    binaryWriter.Write(lastUpdateTime);
                }
            }
        }

        private static void CopyFileOrDirectory(Logger logger, string sourceDir, string root, string path)
        {
            // Root path not handled
            if (root.Length == 0)
                throw new NotSupportedException();

            try
            {
                var zipFile = new ZipFile(sourceDir);
                foreach (var zipEntry in zipFile.GetAllEntries())
                {
                    var zipFilename = zipEntry.FilenameInZip;
                    if (!zipFilename.StartsWith(root))
                        continue;

                    // Get filename without leading "assets/data"
                    var assetFilename = zipFilename.Substring(root.Length);

                    var fullPath = path + assetFilename;
                    var directoryName = VirtualFileSystem.GetParentFolder(fullPath);

                    try
                    {
                        if (directoryName != string.Empty)
                            ApplicationData.CreateDirectory(directoryName);
                    }
                    catch (IOException)
                    {
                    }

                    using (var output = ApplicationData.OpenStream(fullPath, VirtualFileMode.Create, VirtualFileAccess.Write))
                        zipFile.ExtractFile(zipEntry, output);
                }
            }
            catch (IOException ex)
            {
                logger.Info("I/O Exception", ex);
            }
        }
    }
}
#endif
