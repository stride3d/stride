// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Assets;

namespace Xenko.Assets.Media
{
    public class RawSoundAssetImporter : RawAssetImporterBase<SoundAsset>
    {
        // Supported file extensions for this importer
        public const string FileExtensions = ".wav,.mp3,.ogg,.aac,.aiff,.flac,.m4a,.wma,.mpc," + RawVideoAssetImporter.FileExtensions;
        private static readonly Guid Uid = new Guid("634842fa-d1db-45c2-b13d-bc11486dae4d");

        public override Guid Id => Uid;

        public override string Description => "Raw sound importer for creating SoundEffect assets";

        public override string SupportedFileExtensions => FileExtensions;
    }
}
