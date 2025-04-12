// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Graphics;

namespace Stride.Editor.Resources
{
    public static class DefaultThumbnails
    {
        private static readonly Assembly thisAssembly = typeof(DefaultThumbnails).Assembly;

        private static readonly Lazy<Image> lazyAssetBroken = new(
            () => EmbeddedResourceReader.GetImage("Stride.Editor.Resources.appbar.checkmark.cross.png", thisAssembly));
        private static readonly Lazy<Image> lazyDependencyError = new(
            () => EmbeddedResourceReader.GetImage("Stride.Editor.Resources.ThumbnailDependencyError.png", thisAssembly));
        private static readonly Lazy<Image> lazyDependencyWarning = new(
            () => EmbeddedResourceReader.GetImage("Stride.Editor.Resources.ThumbnailDependencyWarning.png", thisAssembly));
        private static readonly Lazy<Image> lazyTextureNoSource = new(
            () => EmbeddedResourceReader.GetImage("Stride.Editor.Resources.appbar.page.delete.png", thisAssembly));
        private static readonly Lazy<Image> lazyUserAsset = new(
            () => EmbeddedResourceReader.GetImage("Stride.Editor.Resources.appbar.resource.png", thisAssembly));

        public static Image AssetBroken => lazyAssetBroken.Value;

        public static Image DependencyError => lazyDependencyError.Value;

        public static Image DependencyWarning => lazyDependencyWarning.Value;

        public static Image TextureNoSource => lazyTextureNoSource.Value;

        public static Image UserAsset => lazyUserAsset.Value;
    }
}
