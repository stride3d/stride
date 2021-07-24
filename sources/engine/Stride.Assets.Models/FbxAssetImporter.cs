// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Assets.Textures;
using Stride.Importer.Common;

namespace Stride.Assets.Models
{
    public class FbxAssetImporter : ModelAssetImporter
    {
        static FbxAssetImporter()
        {
            NativeLibraryHelper.PreloadLibrary("libfbxsdk.dll", typeof(FbxAssetImporter));
        }

        // Supported file extensions for this importer
        private const string FileExtensions = ".fbx";

        private static readonly Guid Uid = new Guid("a15ae42d-42c5-4a3b-9f7e-f8cd91eda595");

        public override Guid Id => Uid;

        public override string Description => "FBX importer used for creating entities, 3D Models or animations assets";

        public override string SupportedFileExtensions => FileExtensions;

        /// <inheritdoc/>
        public override EntityInfo GetEntityInfo(UFile localPath, Logger logger, AssetImporterParameters importParameters)
        {
            var meshConverter = new Importer.FBX.MeshConverter(logger);
            var entityInfo = meshConverter.ExtractEntity(localPath.FullPath, importParameters.IsTypeSelectedForOutput(typeof(TextureAsset)));
            return entityInfo;
        }

        /// <inheritdoc/>
        public override void GetAnimationDuration(UFile localPath, Logger logger, AssetImporterParameters importParameters, out TimeSpan startTime, out TimeSpan endTime)
        {
            var meshConverter = new Importer.FBX.MeshConverter(logger);
            var durationInSeconds = meshConverter.GetAnimationDuration(localPath.FullPath);

            startTime = TimeSpan.Zero;
            endTime = TimeSpan.FromSeconds(durationInSeconds);
        }
    }
}
