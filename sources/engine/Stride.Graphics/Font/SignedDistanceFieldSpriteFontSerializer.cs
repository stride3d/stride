// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// Serializer for <see cref="SignedDistanceFieldSpriteFont"/>.
    /// </summary>
    internal class SignedDistanceFieldSpriteFontSerializer : DataSerializer<SignedDistanceFieldSpriteFont>, IDataSerializerGenericInstantiation
    {
        private DataSerializer<SpriteFont> parentSerializer;

        public override void PreSerialize(ref SignedDistanceFieldSpriteFont texture, ArchiveMode mode, SerializationStream stream)
        {
            // Do not create object during pre-serialize (OK because not recursive)
        }

        public override void Initialize(SerializerSelector serializerSelector)
        {
            parentSerializer = SerializerSelector.Default.GetSerializer<SpriteFont>();
            if (parentSerializer == null)
            {
                throw new InvalidOperationException(string.Format("Could not find parent serializer for type {0}", "Stride.Graphics.SpriteFont"));
            }
        }

        public override void Serialize(ref SignedDistanceFieldSpriteFont font, ArchiveMode mode, SerializationStream stream)
        {
            SpriteFont spriteFont = font;
            parentSerializer.Serialize(ref spriteFont, mode, stream);
            font = (SignedDistanceFieldSpriteFont)spriteFont;

            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var fontSystem = services.GetSafeServiceAs<FontSystem>();

                font.CharacterToGlyph = stream.Read<Dictionary<char, Glyph>>();
                font.StaticTextures = stream.Read<List<Texture>>();

                font.FontSystem = fontSystem;
            }
            else
            {
                stream.Write(font.CharacterToGlyph);
                stream.Write(font.StaticTextures);
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(Dictionary<char, Glyph>));
            genericInstantiations.Add(typeof(List<Texture>));
        }
    }
}
