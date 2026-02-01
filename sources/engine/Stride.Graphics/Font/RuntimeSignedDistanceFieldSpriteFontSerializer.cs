using System;
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Graphics.Font
{
    internal sealed class RuntimeSignedDistanceFieldSpriteFontSerializer : DataSerializer<RuntimeSignedDistanceFieldSpriteFont>
    {
        private DataSerializer<SpriteFont> parentSerializer;

        public override void PreSerialize(ref RuntimeSignedDistanceFieldSpriteFont texture, ArchiveMode mode, SerializationStream stream)
        {
            // Do not create object during pre-serialize (OK because not recursive)
        }

        public override void Initialize(SerializerSelector serializerSelector)
        {
            // Match RuntimeRasterizedSpriteFontSerializer pattern
            parentSerializer = SerializerSelector.Default.GetSerializer<SpriteFont>();
            if (parentSerializer == null)
                throw new InvalidOperationException("Could not find parent serializer for type Stride.Graphics.SpriteFont");
        }

        public override void Serialize(ref RuntimeSignedDistanceFieldSpriteFont font, ArchiveMode mode, SerializationStream stream)
        {
            // Serialize base SpriteFont fields through parent serializer
            SpriteFont spriteFont = font;
            parentSerializer.Serialize(ref spriteFont, mode, stream);
            font = (RuntimeSignedDistanceFieldSpriteFont)spriteFont;

            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var fontSystem = services.GetSafeServiceAs<FontSystem>();

                font.FontName = stream.Read<string>();
                font.Style = stream.Read<FontStyle>();
                font.UseKerning = stream.Read<bool>();

                font.PixelRange = stream.Read<int>();
                font.Padding = stream.Read<int>();

                // Critical: attach runtime FontSystem so caches/fonts work
                font.FontSystem = fontSystem;
            }
            else
            {
                stream.Write(font.FontName);
                stream.Write(font.Style);
                stream.Write(font.UseKerning);

                stream.Write(font.PixelRange);
                stream.Write(font.Padding);
            }
        }
    }
}
