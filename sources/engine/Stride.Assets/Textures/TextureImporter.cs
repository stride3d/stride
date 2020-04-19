// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Assets.Sprite;
using Stride.Assets.Physics;

namespace Stride.Assets.Textures
{
    public class TextureImporter : AssetImporterBase
    {
        // Supported file extensions for this importer
        public const string FileExtensions = ".dds,.jpg,.jpeg,.png,.gif,.bmp,.tga,.psd,.tif,.tiff";

        private static readonly Guid Uid = new Guid("a60986f3-a594-4278-bd9d-68ea172f0558");
        public override Guid Id => Uid;

        public override string Description => "Texture importer for creating Texture assets";

        public override string SupportedFileExtensions => FileExtensions;

        public override IEnumerable<Type> RootAssetTypes
        {
            get
            {
                yield return typeof(TextureAsset);
                yield return typeof(SpriteSheetAsset); // TODO: this is temporary, until we can make the asset templates ask compilers instead of importer which type they support
                yield return typeof(HeightmapAsset);
            }
        }

        public override IEnumerable<AssetItem> Import(UFile rawAssetPath, AssetImporterParameters importParameters)
        {
            var asset = new TextureAsset { Source = rawAssetPath };

            // Creates the url to the texture
            var textureUrl = new UFile(rawAssetPath.GetFileNameWithoutExtension());

            yield return new AssetItem(textureUrl, asset);
        }
    }
}
