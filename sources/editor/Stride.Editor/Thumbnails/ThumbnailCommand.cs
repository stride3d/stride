// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// The base command to build thumbnails.
    /// This command overrides <see cref="GetInputFiles"/> so that it automatically returns all the item asset reference files.
    /// By doing so the thumbnail is re-generated every time one of the dependencies changes.
    /// </summary>
    public abstract class ThumbnailCommand : AssetCommand<ThumbnailCommandParameters>
    {
        private readonly AssetItem assetItem;

        protected ThumbnailCommand(string url, AssetItem assetItem, ThumbnailCommandParameters parameters, IAssetFinder assetFinder)
            : base(url, parameters, assetFinder)
        {
            if (assetItem == null) throw new ArgumentNullException(nameof(assetItem));
            if (assetItem.Package == null) throw new ArgumentException("assetItem is not attached to a package");
            if (assetItem.Package.Session == null) throw new ArgumentException("assetItem is not attached to a package session");
            if (url == null) throw new ArgumentNullException(nameof(url));

            this.assetItem = assetItem;
            //InputFilesGetter = GetInputFilesImpl;
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);
            var dependencies = assetItem.Package.Session.DependencyManager.ComputeDependencies(assetItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
            if (dependencies != null)
            {
                foreach (var assetReference in dependencies.LinksOut)
                {
                    var refAsset = assetReference.Item.Asset;
                    writer.SerializeExtended(ref refAsset, ArchiveMode.Serialize);
                }
            }
        }

//        private IEnumerable<ObjectUrl> GetInputFilesImpl()
//        {
//            var dependencies = assetItem.Package.Session.DependencyManager.ComputeDependencies(assetItem.Id, AssetDependencySearchOptions.Out | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
//            if (dependencies != null)
//            {
//                foreach (var assetReference in dependencies.LinksOut)
//                    yield return new ObjectUrl(UrlType.
//                        ContentLink, assetReference.Item.Location);
//            }
//        }
    }
}
