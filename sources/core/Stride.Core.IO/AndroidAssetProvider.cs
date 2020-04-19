// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_ANDROID
using System.IO;

namespace Stride.Core.IO
{
    /// <summary>
    /// A FileProvider that will load file from Android AssetManager and cache them on the SDCARD for reading.
    /// </summary>
    public class AndroidAssetProvider : VirtualFileProviderBase
    {
        private readonly string assetRoot;
        private FileSystemProvider fileSystemProvider;

        public AndroidAssetProvider(string rootPath, string assetRoot, string cachePath) : base(rootPath)
        {
            fileSystemProvider = new FileSystemProvider(rootPath + "/cache", cachePath);
            this.assetRoot = assetRoot;
        }

        public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamType = StreamFlags.None)
        {
            // For now, block multithreading (not sure it can work or not)
            lock (fileSystemProvider)
            {
                if (!fileSystemProvider.FileExists(url))
                {
                    // Ensure top directory exists
                    fileSystemProvider.CreateDirectory(VirtualFileSystem.GetParentFolder(url));

                    using (var asset = PlatformAndroid.Context.Assets.Open(assetRoot + url))
                    using (var output = fileSystemProvider.OpenStream(url, VirtualFileMode.CreateNew, VirtualFileAccess.Write, share, streamType))
                        asset.CopyTo(output);
                }
            }

            return fileSystemProvider.OpenStream(url, mode, access);
        }

        public override bool FileExists(string url)
        {
            try
            {
                using (var asset = PlatformAndroid.Context.Assets.Open(assetRoot + url))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        public override string GetAbsolutePath(string path)
        {
            return fileSystemProvider.GetAbsolutePath(path);
        }
    }
}
#endif
