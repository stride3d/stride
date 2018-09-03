// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core.Annotations;
using Xenko.Core.IO;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Yaml;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Represents an asset before being loaded. Used mostly for asset upgrading.
    /// </summary>
    public class PackageLoadingAssetFile
    {
        public readonly UDirectory SourceFolder;

        [NotNull] public UFile OriginalFilePath { get; }
        [NotNull] public UFile FilePath { get; set; }

        public long CachedFileSize;

        // If asset has been created or upgraded in place during package upgrade phase, it will be stored here
        public byte[] AssetContent { get; set; }

        public bool Deleted;

        public UFile AssetLocation => FilePath.MakeRelative(SourceFolder).GetDirectoryAndFileNameWithoutExtension();

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageLoadingAssetFile"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="sourceFolder">The source folder.</param>
        public PackageLoadingAssetFile([NotNull] UFile filePath, UDirectory sourceFolder)
        {
            FilePath = filePath;
            OriginalFilePath = FilePath;
            SourceFolder = sourceFolder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageLoadingAssetFile" /> class.
        /// </summary>
        /// <param name="package">The package this asset will be part of.</param>
        /// <param name="filePath">The relative file path (from default asset folder).</param>
        /// <param name="sourceFolder">The source folder (optional, can be null).</param>
        /// <exception cref="System.ArgumentException">filePath must be relative</exception>
        public PackageLoadingAssetFile(Package package, [NotNull] UFile filePath, UDirectory sourceFolder)
        {
            if (filePath.IsAbsolute)
                throw new ArgumentException("filePath must be relative", filePath);

            SourceFolder = UPath.Combine(package.RootDirectory, sourceFolder ?? package.GetDefaultAssetFolder());
            FilePath = UPath.Combine(SourceFolder, filePath);
            OriginalFilePath = FilePath;
        }

        public IReference ToReference()
        {
            return new AssetReference(AssetId.Empty, AssetLocation);
        }

        public YamlAsset AsYamlAsset()
        {
            // The asset file might have been been marked as deleted during the run of asset upgrader. In this case let's just return null.
            if (Deleted)
                return null;

            try
            {
                return new YamlAsset(this);
            }
            catch (SyntaxErrorException)
            {
                return null;
            }
            catch (YamlException)
            {
                return null;
            }
        }

        internal Stream OpenStream()
        {
            if (Deleted)
                throw new InvalidOperationException();

            if (AssetContent != null)
                return new MemoryStream(AssetContent);

            return new FileStream(FilePath.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var result = FilePath.MakeRelative(SourceFolder).ToString();
            if (AssetContent != null)
                result += " (Modified)";
            else if (Deleted)
                result += " (Deleted)";

            return result;
        }

        public class YamlAsset : DynamicYaml, IDisposable
        {
            private readonly PackageLoadingAssetFile packageLoadingAssetFile;

            public YamlAsset(PackageLoadingAssetFile packageLoadingAssetFile) : base(GetSafeStream(packageLoadingAssetFile))
            {
                this.packageLoadingAssetFile = packageLoadingAssetFile;
            }

            public PackageLoadingAssetFile Asset => packageLoadingAssetFile;

            public void Dispose()
            {
                // Save asset back to AssetContent
                using (var memoryStream = new MemoryStream())
                {
                    WriteTo(memoryStream, AssetYamlSerializer.Default.GetSerializerSettings());
                    packageLoadingAssetFile.AssetContent = memoryStream.ToArray();
                }
            }

            private static Stream GetSafeStream(PackageLoadingAssetFile packageLoadingAssetFile)
            {
                if (packageLoadingAssetFile == null) throw new ArgumentNullException(nameof(packageLoadingAssetFile));
                return packageLoadingAssetFile.OpenStream();
            }
        }

        public class FileSizeComparer : Comparer<PackageLoadingAssetFile>
        {
            public new static readonly FileSizeComparer Default = new FileSizeComparer();

            public override int Compare(PackageLoadingAssetFile x, PackageLoadingAssetFile y)
            {
                return y.CachedFileSize.CompareTo(x.CachedFileSize);
            }
        }
    }
}
