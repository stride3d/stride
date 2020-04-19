// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;

namespace Stride.Graphics.Data
{
    internal class TextureImageSerializer : ContentSerializerBase<Texture>
    {
        /// <inheritdoc/>
        public override Type SerializationType => typeof(Image);

        /// <inheritdoc/>
        public override void Serialize(ContentSerializerContext context, SerializationStream stream, Texture texture)
        {
            if (context.Mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var graphicsDeviceService = services.GetSafeServiceAs<IGraphicsDeviceService>();

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
                var textureData = texture.GetSerializationData();
                if (textureData == null)
                    throw new InvalidOperationException("Trying to serialize a Texture without CPU info.");

                textureData.Image.Save(stream.NativeStream, ImageFileType.Stride);
            }
        }

        public override object Construct(ContentSerializerContext context)
        {
            return new Texture();
        }
    }
}
