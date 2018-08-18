// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics;

namespace Xenko.Rendering.RenderTextures
{
    [DataContract]
    [CategoryOrder(10, "Size")]
    [CategoryOrder(20, "Format")]
    [ContentSerializer(typeof(RenderTextureDescriptorContentSerializer))]
    [ContentSerializer(typeof(DataContentSerializer<RenderTextureDescriptor>))]
    public class RenderTextureDescriptor
    {
        /// <summary>
        /// The width in pixel.
        /// </summary>
        [DefaultValue(512)]
        [Display(null, "Size")]
        public int Width { get; set; } = 512;

        /// <summary>
        /// The height in pixel.
        /// </summary>
        [DefaultValue(512)]
        [Display(null, "Size")]
        public int Height { get; set; } = 512;

        /// <summary>
        /// The format.
        /// </summary>
        [DefaultValue(RenderTextureFormat.LDR)]
        [Display("Format", "Format")]
        public RenderTextureFormat Format { get; set; } = RenderTextureFormat.LDR;

        /// <summary>
        /// Gets or sets the value indicating whether the output texture is encoded into the standard RGB color space.
        /// </summary>
        /// <userdoc>
        /// Consider the texture an sRGB image. This should be the default for color textures with HDR/gamma-correct rendering.
        /// </userdoc>
        [DefaultValue(ColorSpace.Linear)]
        [Display("ColorSpace", "Format")]
        public ColorSpace ColorSpace { get; set; } = ColorSpace.Linear;
    }

    internal class RenderTextureDescriptorContentSerializer : ContentSerializerBase<Texture>
    {
        private static readonly DataContentSerializerHelper<RenderTextureDescriptor> DataSerializerHelper = new DataContentSerializerHelper<RenderTextureDescriptor>();

        public override Type SerializationType => typeof(RenderTextureDescriptor);

        public override void Serialize(ContentSerializerContext context, SerializationStream stream, Texture texture)
        {
            if (context.Mode == ArchiveMode.Deserialize)
            {
                var renderTextureDescriptor = new RenderTextureDescriptor();
                DataSerializerHelper.Serialize(context, stream, renderTextureDescriptor);

                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var graphicsDeviceService = services.GetSafeServiceAs<IGraphicsDeviceService>();

                texture.AttachToGraphicsDevice(graphicsDeviceService.GraphicsDevice);

                PixelFormat pixelFormat;
                switch (renderTextureDescriptor.Format)
                {
                    case RenderTextureFormat.LDR:
                        pixelFormat = PixelFormat.R8G8B8A8_UNorm;
                        break;
                    case RenderTextureFormat.HDR:
                        pixelFormat = PixelFormat.R16G16B16A16_Float;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Find color space
                // If linear, use sRGB
                if (renderTextureDescriptor.ColorSpace == ColorSpace.Linear)
                    pixelFormat = pixelFormat.ToSRgb();

                var textureDescription = TextureDescription.New2D(renderTextureDescriptor.Width, renderTextureDescriptor.Height, pixelFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                texture.InitializeFrom(textureDescription, new TextureViewDescription());
            }
            else
            {
                throw new NotSupportedException($"Can't serialize a {nameof(Texture)} back as a {nameof(RenderTextureDescriptor)}");
            }
        }

        public override object Construct(ContentSerializerContext context)
        {
            return new Texture();
        }
    }
}
