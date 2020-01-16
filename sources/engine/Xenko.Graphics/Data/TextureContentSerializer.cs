// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Streaming;

namespace Xenko.Graphics.Data
{
    internal class TextureContentSerializer : ContentSerializerBase<Texture>
    {
        /// <inheritdoc/>
        public override void Serialize(ContentSerializerContext context, SerializationStream stream, Texture texture)
        {
            Serialize(context.Mode, stream, texture, context.AllowContentStreaming);
        }

        internal static void Serialize(ArchiveMode mode, SerializationStream stream, Texture texture, bool allowContentStreaming)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var graphicsDeviceService = services.GetSafeServiceAs<IGraphicsDeviceService>();
                var texturesStreamingProvider = services.GetService<ITexturesStreamingProvider>();

                var isStreamable = stream.ReadBoolean();
                if (!isStreamable)
                {
                    texturesStreamingProvider?.UnregisterTexture(texture);

                    // TODO: Error handling?
                    using (var textureData = Image.Load(stream.NativeStream))
                    {
                        if (texture.GraphicsDevice != null)
                            texture.OnDestroyed(); //Allows fast reloading todo review maybe?

                        texture.AttachToGraphicsDevice(graphicsDeviceService.GraphicsDevice);
                        texture.InitializeFrom(textureData.Description, new TextureViewDescription(), textureData.ToDataBox());

                        // Setup reload callback (reload from asset manager)
                        var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
                        if (contentSerializerContext != null)
                        {
                            var assetManager = contentSerializerContext.ContentManager;
                            var url = contentSerializerContext.Url;

                            texture.Reload = (graphicsResource) =>
                            {
                                var textureDataReloaded = assetManager.Load<Image>(url);
                                ((Texture)graphicsResource).Recreate(textureDataReloaded.ToDataBox());
                                assetManager.Unload(textureDataReloaded);
                            };
                        }
                    }
                }
                else
                {
                    if (texture.GraphicsDevice != null)
                        texture.OnDestroyed();

                    texture.AttachToGraphicsDevice(graphicsDeviceService.GraphicsDevice);
                    texture.Reload = null;

                    // Read image header
                    var imageDescription = new ImageDescription();
                    ImageHelper.ImageDescriptionSerializer.Serialize(ref imageDescription, ArchiveMode.Deserialize, stream);

                    // Read content storage header
                    ContentStorageHeader storageHeader;
                    ContentStorageHeader.Read(stream, out storageHeader);

                    // Check if streaming service is available
                    if (texturesStreamingProvider != null)
                    {
                        if (allowContentStreaming)
                        {
                            // Register texture for streaming
                            texturesStreamingProvider.RegisterTexture(texture, ref imageDescription, ref storageHeader);

                            // Note: here we don't load texture data and don't allocate GPU memory
                        }
                        else
                        {
                            // Request texture loading (should be fully loaded)
                            texturesStreamingProvider.FullyLoadTexture(texture, ref imageDescription, ref storageHeader);
                        }

                        // Load initial texture (with limited number of mipmaps)
                        if (storageHeader.InitialImage)
                        {
                            using (var textureData = Image.Load(stream.NativeStream))
                            {
                                if (texture.GraphicsDevice != null)
                                    texture.OnDestroyed(); //Allows fast reloading todo review maybe?

                                texture.InitializeFrom(textureData.Description, new TextureViewDescription(), textureData.ToDataBox());
                            }
                        }
                    }
                    else
                    {
                        // Load initial texture and discard it (we are going to load the full chunk texture right after)
                        if (storageHeader.InitialImage)
                        {
                            using (var textureData = Image.Load(stream.NativeStream))
                            {
                            }
                        }

                        // Deserialize whole texture without streaming feature
                        var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
                        DeserializeTexture(contentSerializerContext.ContentManager, texture, ref imageDescription, ref storageHeader);
                    }
                }
            }
            else
            {
                var textureData = texture.GetSerializationData();
                if (textureData == null)
                    throw new InvalidOperationException("Trying to serialize a Texture without CPU info.");

                textureData.Write(stream);
            }
        }

        public override object Construct(ContentSerializerContext context)
        {
            return new Texture();
        }

        private static void DeserializeTexture(ContentManager contentManager, Texture texture, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader)
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
                var dataBoxes = new DataBox[imageDescription.MipLevels * imageDescription.ArraySize];
                int dataBoxIndex = 0;

                // Get data boxes data
                for (int arrayIndex = 0; arrayIndex < imageDescription.ArraySize; arrayIndex++)
                {
                    for (int mipIndex = 0; mipIndex < imageDescription.MipLevels; mipIndex++)
                    {
                        int mipWidth = imageDescription.Width >> mipIndex;
                        int mipHeight = imageDescription.Height >> mipIndex;
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

                        dataBoxes[dataBoxIndex].DataPointer = data + slicePitch * arrayIndex;
                        dataBoxes[dataBoxIndex].RowPitch = rowPitch;
                        dataBoxes[dataBoxIndex].SlicePitch = slicePitch;
                        dataBoxIndex++;
                    }
                }

                // Initialize texture
                texture.InitializeFrom(imageDescription, new TextureViewDescription(), dataBoxes);

                storage.UnlockChunks();
            }
        }
    }
}
