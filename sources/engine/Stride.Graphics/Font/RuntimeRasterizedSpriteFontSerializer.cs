// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// Serializer for <see cref="RuntimeRasterizedSpriteFont"/>.
    /// </summary>
    internal class RuntimeRasterizedSpriteFontSerializer : DataSerializer<RuntimeRasterizedSpriteFont>
    {
        private DataSerializer<SpriteFont> parentSerializer;

        public override void PreSerialize(ref RuntimeRasterizedSpriteFont texture, ArchiveMode mode, SerializationStream stream)
        {
            // Do not create object during pre-serialize (OK because not recursive)
        }

        public override void Initialize(SerializerSelector serializerSelector)
        {
            // We should use serializerSelector, but DataContentSerializerHelper we might have wrong context; make sure parent is resolved through proper context
            // (maybe we should have separate contexts for parent and members?)
            parentSerializer = SerializerSelector.Default.GetSerializer<SpriteFont>();
            if (parentSerializer == null)
            {
                throw new InvalidOperationException(string.Format("Could not find parent serializer for type {0}", "Stride.Graphics.SpriteFont"));
            }
        }

        public override void Serialize(ref RuntimeRasterizedSpriteFont font, ArchiveMode mode, SerializationStream stream)
        {
            SpriteFont spriteFont = font;
            parentSerializer.Serialize(ref spriteFont, mode, stream);
            font = (RuntimeRasterizedSpriteFont)spriteFont;

            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var fontSystem = services.GetSafeServiceAs<FontSystem>();

                font.FontName = stream.Read<string>();
                font.Style = stream.Read<FontStyle>();
                font.UseKerning = stream.Read<bool>();
                font.AntiAlias = stream.Read<FontAntiAliasMode>();

                font.FontSystem = fontSystem;
            }
            else
            {
                stream.Write(font.FontName);
                stream.Write(font.Style);
                stream.Write(font.UseKerning);
                stream.Write(font.AntiAlias);
            }
        }
    }
}
