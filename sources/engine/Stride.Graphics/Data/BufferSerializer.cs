// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Graphics.Data
{
    /// <summary>
    /// Serializer for <see cref="Buffer"/>.
    /// </summary>
    public class BufferSerializer : DataSerializer<Buffer>
    {
        public override void PreSerialize(ref Buffer buffer, ArchiveMode mode, SerializationStream stream)
        {
            // Do not create object during preserialize (OK because not recursive)
        }

        public override void Serialize(ref Buffer buffer, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var bufferData = stream.Read<BufferData>();

                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                if (services == null)
                {
                    buffer.SetSerializationData(bufferData);
                }
                else
                {
                    var graphicsDeviceService = services.GetSafeServiceAs<IGraphicsDeviceService>();

                    buffer.AttachToGraphicsDevice(graphicsDeviceService.GraphicsDevice);
                    buffer.InitializeFrom(bufferData.Content, bufferData.StructureByteStride, bufferData.BufferFlags, PixelFormat.None, bufferData.Usage);

                    // Setup reload callback (reload from asset manager)
                    var contentSerializerContext = stream.Context.Get(ContentSerializerContext.ContentSerializerContextProperty);
                    if (contentSerializerContext != null)
                    {
                        buffer.Reload = static (graphicsResource, services) =>
                        {
                            var assetManager = services.GetService<ContentManager>();
                            assetManager.TryGetAssetUrl(graphicsResource, out var url);
                            
                            // When service isn't provided to the ContentManager, deserialized data is stored in a cache instead
                            // of being sent right away to GPU. 
                            // See 'SetSerializationData()' above
                            assetManager = new ContentManager(services.GetService<IDatabaseFileProviderService>());
                            
                            // TODO: Avoid loading/unloading the same data
                            //       ^ perhaps out of date, will need to be discussed
                            var loadedBufferData = assetManager.Load<Buffer>(url);
                            try
                            {
                                var data = loadedBufferData.GetSerializationData().Content;
                                ((Buffer)graphicsResource).Recreate(data);
                            }
                            finally
                            {
                                assetManager.Unload(loadedBufferData);
                            }
                        };
                    }
                }
            }
            else
            {
                var bufferData = buffer.GetSerializationData();
                if (bufferData == null)
                    throw new InvalidOperationException("Trying to serialize a Buffer without CPU info.");

                stream.Write(bufferData);
            }
        }
    }
}
