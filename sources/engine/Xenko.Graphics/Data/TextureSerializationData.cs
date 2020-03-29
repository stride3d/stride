// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1405 // Debug.Assert must provide message text
using System;
using System.Diagnostics;
using Xenko.Core.Annotations;
using Xenko.Core.Serialization;
using Xenko.Core.Streaming;

namespace Xenko.Graphics.Data
{
    /// <summary>
    /// Texture serialization data
    /// </summary>
    public sealed class TextureSerializationData
    {
        internal const int Version = 6;

        /// <summary>
        /// Texture with a mip map count equal or less than this won't use streaming.
        /// Also, those levels will be load during initial load, only lower levels will be streamed.
        /// </summary>
        internal const int InitialNonStreamedMipCount = 6;
        
        /// <summary>
        /// The texture image.
        /// </summary>
        public Image Image;

        /// <summary>
        /// Enables/disables texture streaming.
        /// </summary>
        public bool EnableStreaming;

        /// <summary>
        /// The raw bytes with a content storage header description.
        /// </summary>
        public ContentStorageHeader StorageHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureSerializationData"/> class.
        /// </summary>
        /// <param name="image">The image.</param>
        public TextureSerializationData([NotNull] Image image)
        {
            Image = image;
            EnableStreaming = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureSerializationData"/> class.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="enableStreaming">Enables/disables texture streaming</param>
        /// <param name="storageHeader">Streaming storage data</param>
        public TextureSerializationData([NotNull] Image image, bool enableStreaming, ContentStorageHeader storageHeader)
        {
            Image = image;
            EnableStreaming = enableStreaming;
            StorageHeader = storageHeader;
        }

        /// <summary>
        /// Saves this instance to a stream.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        public void Write(SerializationStream stream)
        {
            var enableStreaming = EnableStreaming && Image.Description.MipLevels > InitialNonStreamedMipCount;

            stream.Write(enableStreaming);
            if (enableStreaming)
            {
                // Write image header
                ImageHelper.ImageDescriptionSerializer.Serialize(ref Image.Description, ArchiveMode.Serialize, stream);

                // Count number of mip maps that won't be part of initial load (they will be available through streaming)
                int skippedMipCount = Image.Description.MipLevels - InitialNonStreamedMipCount;

                // Determine whether we can store initial image
                StorageHeader.InitialImage = true;
                if (Image.Description.Format.IsCompressed())
                {
                    // Compressed: mips need to be multiple of 4, otherwise we can't do it
                    var initialImageWidth = Image.PixelBuffers[skippedMipCount].Width;
                    var initialImageHeight = Image.PixelBuffers[skippedMipCount].Height;
                    if (initialImageWidth % 4 != 0 || initialImageHeight % 4 != 0)
                        StorageHeader.InitialImage = false;
                }

                // Write storage header
                Debug.Assert(!string.IsNullOrEmpty(StorageHeader.DataUrl));
                StorageHeader.Write(stream);

                if (StorageHeader.InitialImage)
                {
                    // Note: in this scenario, we serialize only SkipStreamingMipCount (we know number is strictly higher than this due to previous check)
                    var newDesc = Image.Description;
                    newDesc.MipLevels = InitialNonStreamedMipCount;
                    var pixelBuffers = new PixelBuffer[Image.Description.ArraySize * InitialNonStreamedMipCount];

                    for (uint item = 0; item < Image.Description.ArraySize; ++item)
                    {
                        for (uint level = 0; level < InitialNonStreamedMipCount; ++level)
                        {
                            pixelBuffers[item * InitialNonStreamedMipCount + level] = Image.PixelBuffers[item * Image.Description.MipLevels + level + skippedMipCount];
                        }
                    }

                    // Adjust new Width/Height
                    newDesc.Width = pixelBuffers[0].Width;
                    newDesc.Height = pixelBuffers[0].Height;

                    var initialImage = new Image
                    {
                        Description = newDesc,
                        PixelBuffers = pixelBuffers,
                    };
                    // TODO: We end up duplicating some of the texture data; we could find a way to avoid that by saving only the chunks of higher level mips?
                    initialImage.Save(stream.NativeStream, ImageFileType.Xenko);
                }
            }
            else
            {
                // Write whole image (old texture content serialization)
                Image.Save(stream.NativeStream, ImageFileType.Xenko);
            }
        }
    }
}
