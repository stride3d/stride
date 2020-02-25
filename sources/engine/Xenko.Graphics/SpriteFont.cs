// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics.Font;

using Color = Xenko.Core.Mathematics.Color;
using RectangleF = Xenko.Core.Mathematics.RectangleF;

namespace Xenko.Graphics
{
    /// <summary>
    /// SpriteFont to use with <see cref="SpriteBatch"/>. See <see cref="SpriteFont"/> to learn how to use it.
    /// </summary>
    [DataContract]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<SpriteFont>), Profile = "Content")]
    public class SpriteFont : ComponentBase
    {
        public static readonly Logger Logger = GlobalLogger.GetLogger("SpriteFont");

        // Lookup table indicates which way to move along each axis per SpriteEffects enum value.
        private static readonly Vector2[] AxisDirectionTable =
        {
            new Vector2(-1, -1),
            new Vector2(1, -1),
            new Vector2(-1, 1),
            new Vector2(1, 1),
        };

        // Lookup table indicates which axes are mirrored for each SpriteEffects enum value.
        private static readonly Vector2[] AxisIsMirroredTable =
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        [DataMember(0)]
        internal float BaseOffsetY;

        [DataMember(1)]
        internal float DefaultLineSpacing;

        [DataMember(2)]
        internal Dictionary<int, float> KerningMap;

        /// <summary>
        /// The swizzle mode to use when drawing the sprite font.
        /// </summary>
        protected SwizzleMode swizzle;

        private FontSystem fontSystem;
        private readonly GlyphAction<InternalDrawCommand> internalDrawGlyphAction;
        private readonly GlyphAction<InternalUIDrawCommand> internalUIDrawGlyphAction;
        private readonly GlyphAction<Vector2> measureStringGlyphAction;

        protected internal SpriteFont()
        {
            internalDrawGlyphAction = InternalDrawGlyph;
            internalUIDrawGlyphAction = InternalUIDrawGlyph;
            measureStringGlyphAction = MeasureStringGlyph;
        }

        /// <summary>
        /// Gets the textures containing the font character data.
        /// </summary>
        [DataMemberIgnore]
        public virtual IReadOnlyList<Texture> Textures { get; protected set; }

        /// <summary>
        /// Gets the font size (resp. the default font size) for static fonts (resp. for dynamic fonts) in pixels.
        /// </summary>
        public float Size { get; internal set; }

        /// <summary>
        /// Gets or sets the default character for the font.
        /// </summary>
        public char? DefaultCharacter { get; set; }

        /// <summary>
        /// Completely skips characters that are not in the map.
        /// </summary>
        [DataMemberIgnore]
        public bool IgnoreUnkownCharacters { get; set; }

        /// <summary>
        /// Gets or sets extra spacing (in pixels) between the characters for the current font <see cref="Size"/>. 
        /// This value is scaled during the draw in the case of dynamic fonts. 
        /// Use <see cref="GetExtraSpacing"/> to get the value of the extra spacing for a given font size.
        /// </summary>
        public float ExtraSpacing { get; set; }

        /// <summary>
        /// Gets or sets the extra line spacing (in pixels) to add to the default font line spacing for the current font <see cref="Size"/>.
        /// This value will be scaled during the draw in the case of dynamic fonts.
        /// Use <see cref="GetExtraLineSpacing"/> to get the value of the extra spacing for a given font size.
        /// </summary>
        /// <remarks>Line spacing is the distance between the base lines of two consecutive lines of text (blank space as well as characters' height are thus included).</remarks>
        public float ExtraLineSpacing { get; set; }
        
        /// <summary>
        /// Gets the type indicating if the current font is static, dynamic or SDF.
        /// </summary>
        [DataMemberIgnore]
        public SpriteFontType FontType { get; protected set; }

        /// <summary>
        /// The <see cref="Xenko.Graphics.Font.FontSystem"/> that is managing this sprite font.
        /// </summary>
        [DataMemberIgnore]
        internal virtual FontSystem FontSystem
        {
            get { return fontSystem; }
            set
            {
                if (fontSystem == value)
                    return;

                // unregister itself from the previous font system
                fontSystem?.AllocatedSpriteFonts.Remove(this);

                fontSystem = value;

                // register itself to the new managing font system
                fontSystem?.AllocatedSpriteFonts.Add(this);
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            // unregister itself from its managing system
            FontSystem.AllocatedSpriteFonts.Remove(this);
        }

        public interface IFontManager
        {
            void New();
        }
        
        /// <summary>
        /// Get the value of the extra line spacing for the given font size.
        /// </summary>
        /// <param name="fontSize">The font size in pixels</param>
        /// <returns>The value of the character spacing</returns>
        public virtual float GetExtraSpacing(float fontSize)
        {
            return fontSize / Size * ExtraSpacing;
        }

        /// <summary>
        /// Get the value of the extra character spacing for the given font size.
        /// </summary>
        /// <param name="fontSize">The font size in pixels</param>
        /// <returns>The value of the character spacing</returns>
        public virtual float GetExtraLineSpacing(float fontSize)
        {
            return fontSize / Size * ExtraLineSpacing;
        }

        /// <summary>
        /// Get the value of the font default line spacing for the given font size.
        /// </summary>
        /// <param name="fontSize">The font size in pixels</param>
        /// <returns>The value of the default line spacing</returns>
        public virtual float GetFontDefaultLineSpacing(float fontSize)
        {
            return fontSize / Size * DefaultLineSpacing;
        }

        /// <summary>
        /// Get the value of the base offset for the given font size.
        /// </summary>
        /// <param name="fontSize">The font size in pixels</param>
        /// <returns>The value of the base offset</returns>
        protected virtual float GetBaseOffsetY(float fontSize)
        {
            return fontSize / Size * BaseOffsetY;
        }

        /// <summary>
        /// Gets the value of the total line spacing (font default + user defined) in pixels for a given font size. 
        /// </summary>
        /// <remarks>Line spacing is the distance between the base lines of two consecutive lines of text (blank space as well as characters' height are thus included).</remarks>
        public float GetTotalLineSpacing(float fontSize)
        {
            return GetExtraLineSpacing(fontSize) + GetFontDefaultLineSpacing(fontSize);
        }
        
        internal void InternalDraw(CommandList commandList, ref StringProxy text, ref InternalDrawCommand drawCommand, TextAlignment alignment)
        {
            // If the text is mirrored, offset the start position accordingly.
            if (drawCommand.SpriteEffects != SpriteEffects.None)
            {
                drawCommand.Origin -= MeasureString(ref text, ref drawCommand.FontSize) * AxisIsMirroredTable[(int)drawCommand.SpriteEffects & 3];
            }

            // Draw each character in turn.
            ForEachGlyph(commandList, ref text, ref drawCommand.FontSize, internalDrawGlyphAction, ref drawCommand, alignment, true);
        }        
        
        /// <summary>
        /// Pre-generate synchronously the glyphs of the character needed to render the provided text at the provided size.
        /// </summary>
        /// <param name="text">The text containing the characters to pre-generate</param>
        /// <param name="size">The size of the font</param>
        public void PreGenerateGlyphs(string text, Vector2 size)
        {
            var proxyText = new StringProxy(text);
            PreGenerateGlyphs(ref proxyText, ref size);
        }

        internal virtual void PreGenerateGlyphs(ref StringProxy text, ref Vector2 size)
        {
        }

        internal void InternalDrawGlyph(ref InternalDrawCommand parameters, in Vector2 fontSize, in Glyph glyph, float x, float y, float nextx, ref Vector2 auxiliaryScaling)
        {
            if (char.IsWhiteSpace((char)glyph.Character) || glyph.Subrect.Width == 0 || glyph.Subrect.Height == 0)
                return;

            var spriteEffects = parameters.SpriteEffects;

            var offset = new Vector2(x, y + GetBaseOffsetY(fontSize.Y) + glyph.Offset.Y);
            Vector2.Modulate(ref offset, ref AxisDirectionTable[(int)spriteEffects & 3], out offset);
            Vector2.Add(ref offset, ref parameters.Origin, out offset);
            offset.X = (float)Math.Round(offset.X);
            offset.Y = (float)Math.Round(offset.Y);

            if (spriteEffects != SpriteEffects.None)
            {
                // For mirrored characters, specify bottom and/or right instead of top left.
                var glyphRect = new Vector2(glyph.Subrect.Right - glyph.Subrect.Left, glyph.Subrect.Top - glyph.Subrect.Bottom);
                Vector2.Modulate(ref glyphRect, ref AxisIsMirroredTable[(int)spriteEffects & 3], out offset);
            }
            var destination = new RectangleF(parameters.Position.X, parameters.Position.Y, parameters.Scale.X, parameters.Scale.Y);
            RectangleF? sourceRectangle = glyph.Subrect;
            parameters.SpriteBatch.DrawSprite(Textures[glyph.BitmapIndex], ref destination, true, ref sourceRectangle, parameters.Color, new Color4(0, 0, 0, 0),  parameters.Rotation, ref offset, spriteEffects, ImageOrientation.AsIs, parameters.Depth, swizzle, true);            
        }

        internal void InternalUIDraw(CommandList commandList, ref StringProxy text, ref InternalUIDrawCommand drawCommand)
        {
            // We don't want to have letters with non uniform ratio
            var requestedFontSize = new Vector2(drawCommand.RequestedFontSize * drawCommand.RealVirtualResolutionRatio.Y);

            var textBoxSize = drawCommand.TextBoxSize * drawCommand.RealVirtualResolutionRatio;
            ForEachGlyph(commandList, ref text, ref requestedFontSize, internalUIDrawGlyphAction, ref drawCommand, drawCommand.Alignment, true, textBoxSize);
        }

        internal void InternalUIDrawGlyph(ref InternalUIDrawCommand parameters, in Vector2 requestedFontSize, in Glyph glyph, float x, float y, float nextx, ref Vector2 auxiliaryScaling)
        {
            if (char.IsWhiteSpace((char)glyph.Character))
                return;

            var realVirtualResolutionRatio = requestedFontSize / parameters.RequestedFontSize;

            // Skip items with null size
            var elementSize = new Vector2(
                auxiliaryScaling.X * glyph.Subrect.Width / realVirtualResolutionRatio.X,
                auxiliaryScaling.Y * glyph.Subrect.Height / realVirtualResolutionRatio.Y);
            if (elementSize.Length() < MathUtil.ZeroTolerance) 
                return;

            var xShift = x;
            var yShift = y + (GetBaseOffsetY(requestedFontSize.Y) + glyph.Offset.Y * auxiliaryScaling.Y);
            if (parameters.SnapText)
            {
                xShift = (float)Math.Round(xShift);
                yShift = (float)Math.Round(yShift);
            }
            var xScaledShift = xShift / realVirtualResolutionRatio.X;
            var yScaledShift = yShift / realVirtualResolutionRatio.Y;

            var worldMatrix = parameters.Matrix;

            worldMatrix.M41 += worldMatrix.M11 * xScaledShift + worldMatrix.M21 * yScaledShift;
            worldMatrix.M42 += worldMatrix.M12 * xScaledShift + worldMatrix.M22 * yScaledShift;
            worldMatrix.M43 += worldMatrix.M13 * xScaledShift + worldMatrix.M23 * yScaledShift;
            worldMatrix.M44 += worldMatrix.M14 * xScaledShift + worldMatrix.M24 * yScaledShift;
            
            worldMatrix.M11 *= elementSize.X;
            worldMatrix.M12 *= elementSize.X;
            worldMatrix.M13 *= elementSize.X;
            worldMatrix.M14 *= elementSize.X;
            worldMatrix.M21 *= elementSize.Y;
            worldMatrix.M22 *= elementSize.Y;
            worldMatrix.M23 *= elementSize.Y;
            worldMatrix.M24 *= elementSize.Y;

            RectangleF sourceRectangle = glyph.Subrect;
            parameters.Batch.DrawCharacter(Textures[glyph.BitmapIndex], in worldMatrix, in sourceRectangle, in parameters.Color, parameters.DepthBias, swizzle);
        }

        /// <summary>
        /// Returns the width and height of the provided text for the current font size <see cref="Size"/>
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(string text)
        {
            var fontSize = new Vector2(Size, Size);
            return MeasureString(text, fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for the current font size <see cref="Size"/>
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(StringBuilder text)
        {
            var fontSize = new Vector2(Size, Size);
            return MeasureString(text, fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(string text, float fontSize)
        {
            return MeasureString(text, new Vector2(fontSize, fontSize), text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(StringBuilder text, float fontSize)
        {
            return MeasureString(text, new Vector2(fontSize, fontSize), text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(string text, Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(StringBuilder text, Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(string text, ref Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(StringBuilder text, ref Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <param name="length">The length of the string to measure</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(string text, Vector2 fontSize, int length)
        {
            return MeasureString(text, ref fontSize, length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <param name="length">The length of the string to measure</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(StringBuilder text, Vector2 fontSize, int length)
        {
            return MeasureString(text, ref fontSize, length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <param name="length">The length of the string to measure</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(string text, ref Vector2 fontSize, int length)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var proxy = new StringProxy(text, length);
            return MeasureString(ref proxy, ref fontSize);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <param name="length">The length of the string to measure</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString(StringBuilder text, ref Vector2 fontSize, int length)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var proxy = new StringProxy(text, length);
            return MeasureString(ref proxy, ref fontSize);
        }

        internal Vector2 MeasureString(ref StringProxy text, ref Vector2 size)
        {
            var result = Vector2.Zero;
            ForEachGlyph(null, ref text, ref size, measureStringGlyphAction, ref result, TextAlignment.Left, false); // text size is independent from the text alignment
            return result;
        }

        /// <summary>
        /// Checks whether the provided character is present in the character map of the current <see cref="SpriteFont"/>.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>true if the <paramref name="c"/> is present in the character map, false - otherwise.</returns>
        public virtual bool IsCharPresent(char c)
        {
            return false;
        }

        /// <summary>
        /// Return the glyph associated to provided character at the given size.
        /// </summary>
        /// <param name="commandList">The command list in case we upload gpu resources</param>
        /// <param name="character">The character we want the glyph of</param>
        /// <param name="fontSize">The font size in pixel</param>
        /// <param name="uploadGpuResources">Indicate if the GPU resource should be uploaded or not.</param>
        /// <param name="auxiliaryScaling">If the requested font size isn't available, the closest one is chosen and an auxiliary scaling is returned</param>
        /// <returns>The glyph corresponding to the request or null if not existing</returns>
        protected virtual Glyph GetGlyph(CommandList commandList, char character, in Vector2 fontSize, bool uploadGpuResources, out Vector2 auxiliaryScaling)
        {
            auxiliaryScaling = Vector2.One;
            return null;
        }
        
        private void MeasureStringGlyph(ref Vector2 result, in Vector2 fontSize, in Glyph glyph, float x, float y, float nextx, ref Vector2 auxiliaryScaling)
        {
            // TODO Do we need auxiliaryScaling
            var h = y + GetTotalLineSpacing(fontSize.Y);
            if (nextx > result.X)
            {
                result.X = nextx;
            }
            if (h > result.Y)
            {
                result.Y = h;
            }
        }

        private delegate void GlyphAction<T>(ref T parameters, in Vector2 fontSize, in Glyph glyph, float x, float y, float nextx, ref Vector2 auxiliaryScaling);

        private static int FindCariageReturn(ref StringProxy text, int startIndex)
        {
            var index = startIndex;

            while (index < text.Length && text[index] != '\n')
                ++index;

            return index;
        }

        private void ForEachGlyph<T>(CommandList commandList, ref StringProxy text, ref Vector2 requestedFontSize, GlyphAction<T> action, ref T parameters, TextAlignment scanOrder, bool updateGpuResources, Vector2? textBoxSize = null)
        {
            if (scanOrder == TextAlignment.Left)
            {
                // scan the whole text only one time following the text letter order
                ForGlyph(commandList, ref text, ref requestedFontSize, action, ref parameters, 0, text.Length, updateGpuResources);
            }
            else
            {
                // scan the text line by line incrementing y start position

                // measure the whole string in order to be able to determine xStart
                var wholeSize = textBoxSize ?? MeasureString(ref text, ref requestedFontSize);

                // scan the text line by line
                var yStart = 0f;
                var startIndex = 0;
                var endIndex = FindCariageReturn(ref text, 0);
                while (startIndex < text.Length)
                {
                    // measure the size of the current line
                    var lineSize = Vector2.Zero;
                    ForGlyph(commandList, ref text, ref requestedFontSize, MeasureStringGlyph, ref lineSize, startIndex, endIndex, updateGpuResources);

                    // Determine the start position of the line along the x axis
                    // We round this value to the closest integer to force alignment of all characters to the same pixels
                    // Otherwise the starting offset can fall just in between two pixels and due to float imprecision 
                    // some characters can be aligned to the pixel before and others to the pixel after, resulting in gaps and character overlapping
                    var xStart = (scanOrder == TextAlignment.Center) ? (wholeSize.X - lineSize.X) / 2 : wholeSize.X - lineSize.X;
                    xStart = (float)Math.Round(xStart); 

                    // scan the line
                    ForGlyph(commandList, ref text, ref requestedFontSize, action, ref parameters, startIndex, endIndex, updateGpuResources, xStart, yStart);
                    
                    // update variable before going to next line
                    yStart += GetTotalLineSpacing(requestedFontSize.Y);
                    startIndex = endIndex + 1;
                    endIndex = FindCariageReturn(ref text, startIndex);
                }
            }
        }

        private void ForGlyph<T>(CommandList commandList, ref StringProxy text, ref Vector2 fontSize, GlyphAction<T> action, ref T parameters, int forStart, int forEnd, bool updateGpuResources, float startX = 0, float startY = 0)
        {
            var key = 0;
            var x = startX;
            var y = startY;
            for (var i = forStart; i < forEnd; i++)
            {
                var character = text[i];

                switch (character)
                {
                    case '\r':
                        // Skip carriage returns.
                        key |= character;
                        continue;

                    case '\n':
                        // New line.
                        x = 0;
                        y += GetTotalLineSpacing(fontSize.Y);
                        key |= character;
                        break;

                    default:
                        // Output this character.
                        Vector2 auxiliaryScaling;
                        var glyph = GetGlyph(commandList, character, in fontSize, updateGpuResources, out auxiliaryScaling);
                        if (glyph == null && !IgnoreUnkownCharacters && DefaultCharacter.HasValue)
                            glyph = GetGlyph(commandList, DefaultCharacter.Value, in fontSize, updateGpuResources, out auxiliaryScaling);
                        if (glyph == null)
                            continue;

                        key |= character;

                        var dx = glyph.Offset.X;

                        float kerningOffset;
                        if (KerningMap != null && KerningMap.TryGetValue(key, out kerningOffset))
                            dx += kerningOffset;

                        float nextX = x + (glyph.XAdvance + GetExtraSpacing(fontSize.X)) * auxiliaryScaling.X;
                        action(ref parameters, in fontSize, in glyph, x + dx * auxiliaryScaling.X, y, nextX, ref auxiliaryScaling);
                        x = nextX;
                        break;
                }

                // Shift the kerning key
                key = (key << 16);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StringProxy
        {
            private readonly string textString;
            private readonly StringBuilder textBuilder;
            public readonly int Length;

            public StringProxy(string text)
            {
                textString = text;
                textBuilder = null;
                Length = text.Length;
            }

            public StringProxy(StringBuilder text)
            {
                textBuilder = text;
                textString = null;
                Length = text.Length;
            }
            
            public StringProxy(string text, int length)
            {
                textString = text;
                textBuilder = null;
                Length = Math.Max(0, Math.Min(length, text.Length));
            }

            public StringProxy(StringBuilder text, int length)
            {
                textBuilder = text;
                textString = null;
                Length = Math.Max(0, Math.Min(length, text.Length));
            }

            public bool IsNull => textString == null && textBuilder == null;

            public char this[int index] => textString?[index] ?? textBuilder[index];
        }

        /// <summary>
        /// Structure InternalDrawCommand used to pass parameters to InternalDrawGlyph
        /// </summary>
        internal struct InternalDrawCommand
        {
            public InternalDrawCommand(SpriteBatch spriteBatch, in Vector2 fontSize, in Vector2 position, in Color4 color, float rotation, in Vector2 origin, in Vector2 scale, SpriteEffects spriteEffects, float depth)
            {
                SpriteBatch = spriteBatch;
                Position = position;
                Color = color;
                Rotation = rotation;
                Origin = origin;
                Scale = scale;
                SpriteEffects = spriteEffects;
                Depth = depth;
                FontSize = fontSize;
            }

            public Vector2 FontSize;

            public SpriteBatch SpriteBatch;

            public Vector2 Position;

            public Color4 Color;

            public float Rotation;

            public Vector2 Origin;

            public Vector2 Scale;

            public SpriteEffects SpriteEffects;

            public float Depth;
        }

        /// <summary>
        /// Structure InternalDrawCommand used to pass parameters to InternalDrawGlyph
        /// </summary>
        internal struct InternalUIDrawCommand
        {
            /// <summary>
            /// Font size to be used for the draw command, as requested when the command was issued
            /// </summary>
            public float RequestedFontSize;

            /// <summary>
            /// The ratio between the real and virtual resolution (=real/virtual), inherited from the layouting context
            /// </summary>
            public Vector2 RealVirtualResolutionRatio;

            public UIBatch Batch;

            public Matrix Matrix;

            /// <summary>
            /// The size of the rectangle containing the text
            /// </summary>
            public Vector2 TextBoxSize;

            public Color Color;

            public TextAlignment Alignment;

            public int DepthBias;

            public bool SnapText;
        }
    }
}
