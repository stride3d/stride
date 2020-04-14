// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;

namespace Stride.Graphics.Data
{
    internal class ImageTextureSerializer : ContentSerializerBase<Image>
    {
        /// <inheritdoc/>
        public override Type SerializationType => typeof(Texture);

        public override void Serialize(ContentSerializerContext context, SerializationStream stream, Image textureData)
        {
            if (context.Mode == ArchiveMode.Deserialize)
            {
                var isStreamable = stream.ReadBoolean();
                if (!isStreamable)
                {
                    var image = Image.Load(stream.NativeStream);
                    textureData.InitializeFrom(image);
                }
                else
                {
                    // Read image header
                    var imageDescription = new ImageDescription();
                    ImageHelper.ImageDescriptionSerializer.Serialize(ref imageDescription, ArchiveMode.Deserialize, stream);

                    // Read content storage header
                    ContentStorageHeader storageHeader;
                    ContentStorageHeader.Read(stream, out storageHeader);

                    // Deserialize whole texture to image without streaming feature
                    var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
                    DeserializeImage(contentSerializerContext.ContentManager, textureData, ref imageDescription, ref storageHeader);
                }
            }
            else
            {
                textureData.Save(stream.NativeStream, ImageFileType.Stride);
            }
        }

        public override object Construct(ContentSerializerContext context)
        {
            return new Image();
        }

        private static void DeserializeImage(ContentManager contentManager, Image obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader)
        {
            using (var content = new ContentStreamingService())
            {
                // Get content storage container
                var storage = content.GetStorage(ref storageHeader);
                if (storage == null)
                    throw new ContentStreamingException("Missing content storage.");
                storage.LockChunks();

                // Cache data
                var fileProvider = contentManager.FileProvider;
                var format = imageDescription.Format;
                bool isBlockCompressed =
                    (format >= PixelFormat.BC1_Typeless && format <= PixelFormat.BC5_SNorm) ||
                    (format >= PixelFormat.BC6H_Typeless && format <= PixelFormat.BC7_UNorm_SRgb);

                // Calculate total size
                int size = 0;
                for (int mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++)
                {
                    int mipWidth = Math.Max(1, imageDescription.Width >> mipIndex);
                    int mipHeight = Math.Max(1, imageDescription.Height >> mipIndex);
                    if (isBlockCompressed && ((mipWidth % 4) != 0 || (mipHeight % 4) != 0))
                    {
                        mipWidth = unchecked((int)(((uint)(mipWidth + 3)) & ~3U));
                        mipHeight = unchecked((int)(((uint)(mipHeight + 3)) & ~3U));
                    }

                    int rowPitch, slicePitch;
                    int widthPacked;
                    int heightPacked;
                    Image.ComputePitch(format, mipWidth, mipHeight, out rowPitch, out slicePitch, out widthPacked, out heightPacked);

                    size += slicePitch;
                }
                size *= imageDescription.ArraySize;

                // Preload chunks
                for (int mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++)
                    storage.GetChunk(mipIndex)?.GetData(fileProvider);

                // Allocate buffer for image data
                var buffer = Utilities.AllocateMemory(size);

                try
                {
                    // Load image data to the buffer
                    var bufferPtr = buffer;
                    for (int arrayIndex = 0; arrayIndex < imageDescription.ArraySize; arrayIndex++)
                    {
                        for (int mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++)
                        {
                            int mipWidth = Math.Max(1, imageDescription.Width >> mipIndex);
                            int mipHeight = Math.Max(1, imageDescription.Height >> mipIndex);
                            if (isBlockCompressed && ((mipWidth % 4) != 0 || (mipHeight % 4) != 0))
                            {
                                mipWidth = unchecked((int)(((uint)(mipWidth + 3)) & ~3U));
                                mipHeight = unchecked((int)(((uint)(mipHeight + 3)) & ~3U));
                            }

                            int rowPitch, slicePitch;
                            int widthPacked;
                            int heightPacked;
                            Image.ComputePitch(format, mipWidth, mipHeight, out rowPitch, out slicePitch, out widthPacked, out heightPacked);
                            var chunk = storage.GetChunk(mipIndex);
                            if (chunk == null || chunk.Size != slicePitch * imageDescription.ArraySize)
                                throw new ContentStreamingException("Data chunk is missing or has invalid size.", storage);
                            var data = chunk.GetData(fileProvider);
                            if (!chunk.IsLoaded)
                                throw new ContentStreamingException("Data chunk is not loaded.", storage);
                                
                            Utilities.CopyMemory(bufferPtr, data, chunk.Size);
                            bufferPtr += chunk.Size;
                        }
                    }

                    // Initialize image
                    var image = new Image(imageDescription, buffer, 0, null, true);
                    obj.InitializeFrom(image);
                }
                catch
                {
                    // Free memory in case of error
                    Utilities.FreeMemory(buffer);

                    throw;
                }

                storage.UnlockChunks();
            }
        }
    }
}
