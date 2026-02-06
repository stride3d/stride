// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Font;

using Color = Stride.Core.Mathematics.Color;
using RectangleF = Stride.Core.Mathematics.RectangleF;

namespace Stride.Graphics
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

        /// <summary>
        /// Gets the textures containing the font character data.
        /// </summary>
        [DataMemberIgnore]
        public virtual IReadOnlyList<Texture> Textures { get; protected set; }

        /// <summary>
        /// Gets the font size (resp. the default font size) for static fonts (resp. for dynamic fonts) in pixels.
        /// </summary>
        [DataMember]
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
        /// The <see cref="Stride.Graphics.Font.FontSystem"/> that is managing this sprite font.
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
        
        internal void InternalDraw(CommandList commandList, in StringProxy text, ref InternalDrawCommand drawCommand, TextAlignment alignment)
        {
            // If the text is mirrored, offset the start position accordingly.
            if (drawCommand.SpriteEffects != SpriteEffects.None)
            {
                drawCommand.Origin -= MeasureString(text, drawCommand.FontSize) * AxisIsMirroredTable[(int)drawCommand.SpriteEffects & 3];
            }

            (TextAlignment alignment, Vector2 textboxSize)? scanOption = alignment == TextAlignment.Left ? null : (alignment, MeasureString(text, drawCommand.FontSize));
            
            // Draw each character in turn.
            foreach (var glyphInfo in new GlyphEnumerator(commandList, text, drawCommand.FontSize, true, 0, text.Length, this, scanOption))
            {
                InternalDrawGlyph(ref drawCommand, in drawCommand.FontSize, glyphInfo);
            }
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

        internal void InternalDrawGlyph(ref InternalDrawCommand parameters, in Vector2 fontSize, in GlyphPosition glyphPosition)
        {
            if (char.IsWhiteSpace((char)glyphPosition.Glyph.Character) || glyphPosition.Glyph.Subrect.Width == 0 || glyphPosition.Glyph.Subrect.Height == 0)
                return;

            var spriteEffects = parameters.SpriteEffects;

            var offset = new Vector2(glyphPosition.X, glyphPosition.Y + GetBaseOffsetY(fontSize.Y) + glyphPosition.Glyph.Offset.Y);
            Vector2.Modulate(ref offset, ref AxisDirectionTable[(int)spriteEffects & 3], out offset);
            Vector2.Add(ref offset, ref parameters.Origin, out offset);
            offset.X = MathF.Round(offset.X);
            offset.Y = MathF.Round(offset.Y);

            if (spriteEffects != SpriteEffects.None)
            {
                // For mirrored characters, specify bottom and/or right instead of top left.
                var glyphRect = new Vector2(glyphPosition.Glyph.Subrect.Right - glyphPosition.Glyph.Subrect.Left, glyphPosition.Glyph.Subrect.Top - glyphPosition.Glyph.Subrect.Bottom);
                Vector2.Modulate(ref glyphRect, ref AxisIsMirroredTable[(int)spriteEffects & 3], out offset);
            }
            var destination = new RectangleF(parameters.Position.X, parameters.Position.Y, parameters.Scale.X, parameters.Scale.Y);
            RectangleF? sourceRectangle = glyphPosition.Glyph.Subrect;
            parameters.SpriteBatch.DrawSprite(Textures[glyphPosition.Glyph.BitmapIndex], ref destination, true, ref sourceRectangle, parameters.Color, new Color4(0, 0, 0, 0), parameters.Rotation, ref offset, spriteEffects, ImageOrientation.AsIs, parameters.Depth, swizzle, true);            
        }

        internal void InternalUIDraw(CommandList commandList, in StringProxy text, ref InternalUIDrawCommand drawCommand, in Vector2 actualFontSize)
        {
            // We don't want to have letters with non uniform ratio

            var textBoxSize = drawCommand.TextBoxSize * drawCommand.RealVirtualResolutionRatio;
            foreach (var glyphInfo in new GlyphEnumerator(commandList, text, actualFontSize, true, 0, text.Length, this, (drawCommand.Alignment, textBoxSize)))
            {
                InternalUIDrawGlyph(ref drawCommand, in actualFontSize, glyphInfo);
            }
        }

        internal void InternalUIDrawGlyph(ref InternalUIDrawCommand parameters, in Vector2 requestedFontSize, in GlyphPosition glyphPosition)
        {
            if (char.IsWhiteSpace((char)glyphPosition.Glyph.Character))
                return;

            var realVirtualResolutionRatio = requestedFontSize / parameters.RequestedFontSize;

            // Skip items with null size
            var elementSize = new Vector2(
                glyphPosition.AuxiliaryScaling.X * glyphPosition.Glyph.Subrect.Width / realVirtualResolutionRatio.X,
                glyphPosition.AuxiliaryScaling.Y * glyphPosition.Glyph.Subrect.Height / realVirtualResolutionRatio.Y);
            if (elementSize.LengthSquared() < MathUtil.ZeroTolerance) 
                return;

            var xShift = glyphPosition.X;
            var yShift = glyphPosition.Y + (GetBaseOffsetY(requestedFontSize.Y) + glyphPosition.Glyph.Offset.Y * glyphPosition.AuxiliaryScaling.Y);
            if (parameters.SnapText)
            {
                xShift = MathF.Round(xShift);
                yShift = MathF.Round(yShift);
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

            RectangleF sourceRectangle = glyphPosition.Glyph.Subrect;
            parameters.Batch.DrawCharacter(Textures[glyphPosition.Glyph.BitmapIndex], in worldMatrix, in sourceRectangle, in parameters.Color, parameters.DepthBias, swizzle);
        }

        public int IndexInString([NotNull] string text, in Vector2 fontSize, Vector2 pointOnText, (TextAlignment text, Vector2 boxSize)? scanOption)
        {
            pointOnText.Y -= GetTotalLineSpacing(fontSize.Y); // Characters go from 0->+Y downwards, 
            var proxy = new StringProxy(text, text.Length);
            (int index, float score, float x) = (0, float.PositiveInfinity, 0);
            foreach (var glyphInfo in new GlyphEnumerator(null, proxy, fontSize, false, 0, text.Length, this, scanOption))
            {
                var sqrd = Vector2.DistanceSquared(new Vector2(glyphInfo.X, glyphInfo.Y), pointOnText);
                if (sqrd < score)
                {
                    index = glyphInfo.index;
                    score = sqrd;
                    x = glyphInfo.X * 0.5f + glyphInfo.NextX * 0.5f;
                }
            }

            if (index == text.Length - 1 && x < pointOnText.X)
            {
                return text.Length;
            }

            return index;
        }

        /// <summary>
        /// Returns the width and height of the provided text for the current font size <see cref="Size"/>
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] string text)
        {
            var fontSize = new Vector2(Size, Size);
            return MeasureString(text, fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for the current font size <see cref="Size"/>
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] StringBuilder text)
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
        public Vector2 MeasureString([NotNull] string text, float fontSize)
        {
            return MeasureString(text, new Vector2(fontSize, fontSize), text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] StringBuilder text, float fontSize)
        {
            return MeasureString(text, new Vector2(fontSize, fontSize), text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] string text, Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] StringBuilder text, Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] string text, ref Vector2 fontSize)
        {
            return MeasureString(text, ref fontSize, text.Length);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] StringBuilder text, ref Vector2 fontSize)
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
        public Vector2 MeasureString([NotNull] string text, Vector2 fontSize, int length)
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
        public Vector2 MeasureString([NotNull] StringBuilder text, Vector2 fontSize, int length)
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
        public Vector2 MeasureString([NotNull] string text, ref Vector2 fontSize, int length)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var proxy = new StringProxy(text, length);
            return MeasureString(proxy, fontSize);
        }

        /// <summary>
        /// Returns the width and height of the provided text for a given font size
        /// </summary>
        /// <param name="text">The string to measure.</param>
        /// <param name="fontSize">The size of the font (ignored in the case of static fonts)</param>
        /// <param name="length">The length of the string to measure</param>
        /// <returns>Vector2.</returns>
        public Vector2 MeasureString([NotNull] StringBuilder text, ref Vector2 fontSize, int length)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var proxy = new StringProxy(text, length);
            return MeasureString(proxy, fontSize);
        }

        internal Vector2 MeasureString(in StringProxy text, in Vector2 size)
        {
            var result = Vector2.Zero;
            foreach (var glyphInfo in new GlyphEnumerator(null, text, size, false, 0, text.Length, this))
            {
                MeasureStringGlyph(ref result, in size, glyphInfo);
            }
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

        internal void TypeSpecificRatios(float requestedFontSize, ref bool snapText, ref Vector2 realVirtualResolutionRatio, out Vector2 actualFontSize)
        {
            if (FontType == SpriteFontType.SDF)
            {
                snapText = false;
                float scaling = requestedFontSize / Size;
                realVirtualResolutionRatio = 1 / new Vector2(scaling, scaling);
            }
            if (FontType == SpriteFontType.Static)
            {
                realVirtualResolutionRatio = Vector2.One; // ensure that static font are not scaled internally
            }
            if (FontType == SpriteFontType.Dynamic)
            {
                // Dynamic: if we're not displaying in a situation where we can snap text, we're probably in 3D.
                // Let's use virtual resolution (otherwise requested size might change on every camera move)
                // TODO: some step function to have LOD without regenerating on every small change?
                if (!snapText)
                    realVirtualResolutionRatio = Vector2.One;
            }
            
            actualFontSize = new Vector2(realVirtualResolutionRatio.Y * requestedFontSize); // we don't want letters non-uniform ratio
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
        
        internal void MeasureStringGlyph(ref Vector2 result, in Vector2 fontSize, in GlyphPosition glyphPosition)
        {
            // TODO Do we need auxiliaryScaling
            var h = glyphPosition.Y + GetTotalLineSpacing(fontSize.Y);
            if (glyphPosition.NextX > result.X)
            {
                result.X = glyphPosition.NextX;
            }
            if (h > result.Y)
            {
                result.Y = h;
            }
        }

        public record struct GlyphPosition(Glyph Glyph, float X, float Y, float NextX, int index, Vector2 AuxiliaryScaling);

        internal struct GlyphEnumerator : IEnumerator<GlyphPosition>, IEnumerable<GlyphPosition>
        {
            private int index;
            private int key;
            private float x;
            private float y;
            private int forEnd;
            private Vector2 textboxSize;
            private GlyphPosition current;
            private readonly bool updateGpuResources;
            private readonly TextAlignment scanOrder;
            private readonly StringProxy text;
            private readonly Vector2 fontSize;
            [CanBeNull] private readonly CommandList commandList;
            [NotNull] private readonly SpriteFont font;

            public GlyphEnumerator(
                [CanBeNull] CommandList commandList, 
                StringProxy text, 
                Vector2 fontSize, 
                bool updateGpuResources,
                int forStart, 
                int forEnd, 
                [NotNull] SpriteFont font,
                (TextAlignment alignment, Vector2 textboxSize)? scanOptions = null)
            {
                this.commandList = commandList;
                this.text = text;
                this.fontSize = fontSize;
                this.updateGpuResources = updateGpuResources;
                this.forEnd = forEnd;
                this.font = font;
                this.scanOrder = scanOptions?.alignment ?? TextAlignment.Left;
                textboxSize = scanOptions?.textboxSize ?? default;
                index = forStart;
                y = 0;
                x = FindHorizontalOffset(index);
            }

            public bool MoveNext()
            {
                while (index < forEnd)
                {
                    char character = text[index];
                    index++;

                    if (character == '\r')
                        continue;

                    var currentKey = key;
                    key |= character;
                    key = (key << 16);

                    switch (character)
                    {
                        case '\n':
                            x = FindHorizontalOffset(index);
                            y += font.GetTotalLineSpacing(fontSize.Y);
                            break;

                        default:
                            Vector2 auxiliaryScaling;
                            var glyph = font.GetGlyph(commandList, character, in fontSize, updateGpuResources, out auxiliaryScaling);
                            if (glyph == null && !font.IgnoreUnkownCharacters && font.DefaultCharacter.HasValue)
                                glyph = font.GetGlyph(commandList, font.DefaultCharacter.Value, in fontSize, updateGpuResources, out auxiliaryScaling);
                            if (glyph == null)
                                break;

                            var dx = glyph.Offset.X;
                            if (font.KerningMap != null && font.KerningMap.TryGetValue(currentKey, out var kerningOffset))
                                dx += kerningOffset;

                            float nextX = x + (glyph.XAdvance + font.GetExtraSpacing(fontSize.X)) * auxiliaryScaling.X;
                            current = new(glyph, x + dx * auxiliaryScaling.X, y, nextX, index - 1, auxiliaryScaling);
                            x = nextX;
                            return true;
                    }
                }

                return false;
            }

            private float FindHorizontalOffset(int scanStart)
            {
                if (scanOrder == TextAlignment.Left)
                {
                    return 0;
                }

                var nextLine = scanStart;
                while (nextLine < text.Length && text[nextLine] != '\n')
                    ++nextLine;

                var lineSize = Vector2.Zero;
                foreach (var glyphInfo in new GlyphEnumerator(commandList, text, fontSize, updateGpuResources, scanStart, nextLine, font))
                {
                    font.MeasureStringGlyph(ref lineSize, in fontSize, glyphInfo);
                }

                // Determine the start position of the line along the x axis
                // We round this value to the closest integer to force alignment of all characters to the same pixels
                // Otherwise the starting offset can fall just in between two pixels and due to float imprecision 
                // some characters can be aligned to the pixel before and others to the pixel after, resulting in gaps and character overlapping
                var xStart = (scanOrder == TextAlignment.Center) ? (textboxSize.X - lineSize.X) / 2 : textboxSize.X - lineSize.X;
                xStart = MathF.Round(xStart);
                return xStart;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose() { }

            public GlyphPosition Current => current;
            [NotNull] object IEnumerator.Current => current;

            public GlyphEnumerator GetEnumerator() => this;
            IEnumerator<GlyphPosition> IEnumerable<GlyphPosition>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct StringProxy
        {
            private readonly string textString;
            private readonly StringBuilder textBuilder;
            public readonly int Length;

            public StringProxy([NotNull] string text)
            {
                textString = text;
                textBuilder = null;
                Length = text.Length;
            }

            public StringProxy([NotNull] StringBuilder text)
            {
                textBuilder = text;
                textString = null;
                Length = text.Length;
            }
            
            public StringProxy([NotNull] string text, int length)
            {
                textString = text;
                textBuilder = null;
                Length = Math.Max(0, Math.Min(length, text.Length));
            }

            public StringProxy([NotNull] StringBuilder text, int length)
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
