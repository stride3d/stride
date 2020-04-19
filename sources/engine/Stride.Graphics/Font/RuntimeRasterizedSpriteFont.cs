// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// A dynamic font. That is a font that generate its character bitmaps at execution.
    /// </summary>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<RuntimeRasterizedSpriteFont>), Profile = "Content")]
    [ContentSerializer(typeof(RuntimeRasterizedSpriteFontContentSerializer))]
    [DataSerializer(typeof(RuntimeRasterizedSpriteFontSerializer))]
    internal class RuntimeRasterizedSpriteFont : SpriteFont
    {
        /// <summary>
        /// Input the family name of the (TrueType) font.
        /// </summary>
        internal string FontName;

        /// <summary>
        /// Style for the font. 'regular', 'bold' or 'italic'. Default is 'regular
        /// </summary>
        internal FontStyle Style;

        /// <summary>
        /// Specifies whether to use kerning information when rendering the font. Default value is false (NOT SUPPORTED YET).
        /// </summary>
        internal bool UseKerning;

        /// <summary>
        /// The alias mode of the font
        /// </summary>
        internal FontAntiAliasMode AntiAlias;

        /// <summary>
        /// The character specifications cached to avoid re-allocations
        /// </summary>
        private readonly Dictionary<CharacterKey, CharacterSpecification> sizedCharacterToCharacterData = new Dictionary<CharacterKey, CharacterSpecification>();

        [DataMemberIgnore]
        internal FontManager FontManager
        {
            get { return FontSystem != null ? FontSystem.FontManager : null; }
        }

        [DataMemberIgnore]
        internal FontCacheManager FontCacheManager
        {
            get { return FontSystem != null ? FontSystem.FontCacheManager : null; }
        }

        [DataMemberIgnore]
        internal int FrameCount
        {
            get { return FontSystem != null ? FontSystem.FrameCount : 0; }
        }

        [DataMemberIgnore]
        internal override FontSystem FontSystem
        {
            set
            {
                if (FontSystem == value)
                    return;

                base.FontSystem = value;
                
                // retrieve needed info from the font
                float relativeLineSpacing;
                float relativeBaseOffsetY;
                float relativeMaxWidth;
                float relativeMaxHeight;
                FontManager.GetFontInfo(FontName, Style, out relativeLineSpacing, out relativeBaseOffsetY, out relativeMaxWidth, out relativeMaxHeight);

                // set required base properties
                DefaultLineSpacing = relativeLineSpacing * Size;
                BaseOffsetY = relativeBaseOffsetY * Size;
                Textures = FontCacheManager.Textures;
                swizzle = SwizzleMode.RRRR;
            }
        }

        public RuntimeRasterizedSpriteFont()
        {
            FontType = SpriteFontType.Dynamic;
        }

        public override bool IsCharPresent(char c)
        {
            return FontManager.DoesFontContains(FontName, Style, c);
        }

        protected override Glyph GetGlyph(CommandList commandList, char character, in Vector2 fontSize, bool uploadGpuResources, out Vector2 fixScaling)
        {
            // Add a safe guard to prevent the system to generate characters too big for the dynamic font cache texture
            var realFontSize = fontSize;
            realFontSize.X = Math.Min(realFontSize.X, 256);
            realFontSize.Y = Math.Min(realFontSize.Y, 256);

            // get the character data associated to the provided character and size
            var characterData = GetOrCreateCharacterData(realFontSize, character);

            // generate the bitmap if it does not exist
            if (characterData.Bitmap == null)
            {
                FontManager.GenerateBitmap(characterData, false);

                // TODO: try to find a fallback from different size in the meantime (currently character disappear)
            }

            // upload the character to the GPU font texture and create the glyph if does not exists
            if (uploadGpuResources && characterData.Bitmap != null && !characterData.IsBitmapUploaded)
                FontCacheManager.UploadCharacterBitmap(commandList, characterData);

            // update the character usage info
            FontCacheManager.NotifyCharacterUtilization(characterData);

            fixScaling = fontSize / characterData.Size;

            return characterData.Glyph;
        }

        internal override void PreGenerateGlyphs(ref StringProxy text, ref Vector2 size)
        {
            for (int i = 0; i < text.Length; i++)
            {
                // get the character data associated to the provided character and size
                var characterData = GetOrCreateCharacterData(size, text[i]);

                // force asynchronous generation of the bitmap if it does not exist
                if (characterData.Bitmap == null)
                    FontManager.GenerateBitmap(characterData, true);
            }
        }

        private CharacterSpecification GetOrCreateCharacterData(Vector2 size, char character)
        {
            // build the dictionary look up key
            var lookUpKey = new CharacterKey(character, size);

            // get the entry (creates it if it does not exist)
            CharacterSpecification characterData;
            if (!sizedCharacterToCharacterData.TryGetValue(lookUpKey, out characterData))
            {
                characterData = new CharacterSpecification(character, FontName, size, Style, AntiAlias);
                sizedCharacterToCharacterData[lookUpKey] = characterData;
            }

            return characterData;
        }

        private struct CharacterKey : IEquatable<CharacterKey>
        {
            private readonly char character;

            private readonly int sizeX;
            private readonly int sizeY;

            public CharacterKey(char character, Vector2 size)
            {
                this.character = character;
                this.sizeX = (int)size.X;
                this.sizeY = (int)size.Y;
            }

            public bool Equals(CharacterKey other)
            {
                return (character == other.character) && (sizeX == other.sizeX) && (sizeY == other.sizeY);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is CharacterKey && Equals((CharacterKey)obj);
            }

            public override int GetHashCode()
            {
                return character.GetHashCode();
            }
        }
    }
}
