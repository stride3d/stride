// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to load a texture. Use one of the concrete subclasses depending on the source:
    /// <see cref="FileLoadingRequest"/>, <see cref="TexImageLoadingRequest"/>, or
    /// <see cref="XkImageLoadingRequest"/>.
    /// </summary>
    internal abstract class LoadingRequest : IRequest
    {
        public override RequestType Type => RequestType.Loading;

        /// <summary>
        /// Indicate if we should keep the original mip-maps during the load.
        /// </summary>
        public bool KeepMipMap { get; set; }

        /// <summary>
        /// Indicate if the input should be loaded as an sRGB image.
        /// </summary>
        public bool LoadAsSRgb { get; }

        protected LoadingRequest(bool loadAsSRgb)
        {
            LoadAsSRgb = loadAsSRgb;
        }
    }

    /// <summary>
    /// Load a texture from a file on disk.
    /// </summary>
    internal sealed class FileLoadingRequest : LoadingRequest
    {
        public string FilePath { get; }

        public FileLoadingRequest(string filePath, bool loadAsSRgb) : base(loadAsSRgb)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Load a texture from an in-memory <see cref="TexImage"/> instance.
    /// </summary>
    internal sealed class TexImageLoadingRequest : LoadingRequest
    {
        public TexImage Image { get; }

        public TexImageLoadingRequest(TexImage image, bool loadAsSRgb) : base(loadAsSRgb)
        {
            Image = image;
        }
    }

    /// <summary>
    /// Load a texture from an in-memory <see cref="Stride.Graphics.Image"/> instance.
    /// </summary>
    internal sealed class XkImageLoadingRequest : LoadingRequest
    {
        public Stride.Graphics.Image XkImage { get; }

        public XkImageLoadingRequest(Stride.Graphics.Image xkImage, bool loadAsSRgb) : base(loadAsSRgb)
        {
            XkImage = xkImage;
        }
    }
}
