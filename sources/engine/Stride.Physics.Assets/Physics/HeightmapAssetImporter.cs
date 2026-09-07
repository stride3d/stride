// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Assets;

namespace Stride.Physics.Assets
{
    /// <summary>
    /// Creates a <see cref="HeightmapAsset"/> from an image file (the same formats as a texture).
    /// </summary>
    public class HeightmapAssetImporter : RawAssetImporterBase<HeightmapAsset>
    {
        private static readonly Guid Uid = new Guid("d2f5a7f1-0f6b-4b32-9a9f-3c5c1d3b7e21");

        public override Guid Id => Uid;

        public override string Description => "Heightmap importer for creating Heightmap assets from images";

        public override string SupportedFileExtensions => TextureImporter.FileExtensions;
    }
}
