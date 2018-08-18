// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1405 // Debug.Assert must provide message text
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
        internal const int Version = 4;
        
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
            stream.Write(EnableStreaming);
            if (EnableStreaming)
            {
                // Write image header
                ImageHelper.ImageDescriptionSerializer.Serialize(ref Image.Description, ArchiveMode.Serialize, stream);

                // Write storage header
                Debug.Assert(!string.IsNullOrEmpty(StorageHeader.DataUrl));
                StorageHeader.Write(stream);
            }
            else
            {
                // Write whole image (old texture content serialization)
                Image.Save(stream.NativeStream, ImageFileType.Xenko);
            }
        }
    }
}
