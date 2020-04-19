// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to load a texture, either from a file, or from memory with an <see cref="TexImage"/> or a <see cref="Stride.Graphics.Image"/>
    /// </summary>
    internal class LoadingRequest : IRequest
    {
        /// <summary>
        /// The different loading mode : TexImage, file, Stride Image
        /// </summary>
        public enum LoadingMode
        {
            TexImage,
            XkImage,
            FilePath,
        }

        public override RequestType Type { get { return RequestType.Loading; } }

        /// <summary>
        /// The mode used by the request
        /// </summary>
        public LoadingMode Mode { set; get; }
        
        /// <summary>
        /// The file path
        /// </summary>
        public String FilePath { set; get; }
        
        /// <summary>
        /// The TexImage to be loaded
        /// </summary>
        public TexImage Image { set; get; }

        /// <summary>
        /// The Stride Image to be loaded
        /// </summary>
        public Stride.Graphics.Image XkImage;

        /// <summary>
        /// Indicate if we should keep the original mip-maps during the load
        /// </summary>
        public bool KeepMipMap { get; set; }

        /// <summary>
        /// Indicate if the input file should be loaded as an sRGB file.
        /// </summary>
        public bool LoadAsSRgb { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingRequest"/> class to load a texture from a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="loadAsSRgb">Indicate if the input file should be loaded as in sRGB file</param>
        public LoadingRequest(String filePath, bool loadAsSRgb)
        {
            FilePath = filePath;
            Mode = LoadingMode.FilePath;
            LoadAsSRgb = loadAsSRgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingRequest"/> class to load a texture from a <see cref="TexImage"/> instance.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="loadAsSRgb">Indicate if the input file should be loaded as in sRGB file</param>
        public LoadingRequest(TexImage image, bool loadAsSRgb)
        {
            Image = image;
            Mode = LoadingMode.TexImage;
            LoadAsSRgb = loadAsSRgb;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingRequest"/> class to load a texture from a <see cref="Stride.Graphics.Image"/> instance.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="loadAsSRgb">Indicate if the input file should be loaded as in sRGB file</param>
        public LoadingRequest(Stride.Graphics.Image image, bool loadAsSRgb)
        {
            XkImage = image;
            Mode = LoadingMode.XkImage;
            LoadAsSRgb = loadAsSRgb;
        }
    }
}
