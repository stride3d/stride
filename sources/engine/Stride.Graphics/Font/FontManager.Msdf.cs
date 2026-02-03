// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using SharpFont;
using Stride.Core.Mathematics;
using Stride.Graphics.Font.RuntimeMsdf;

namespace Stride.Graphics.Font
{
    internal partial class FontManager
    {
        /// <summary>
        /// Extracts a glyph outline (vector shape) for MSDF generation.
        ///
        /// This is intentionally synchronous and protected by the same FreeType lock as bitmap generation.
        /// If you later want more serialization/perf control, route this request through the existing
        /// bitmap builder thread and return a copied <see cref="GlyphOutline"/>.
        /// </summary>
        public bool TryGetGlyphOutline(
            string fontFamily,
            FontStyle fontStyle,
            Vector2 size,
            char character,
            out GlyphOutline outline,
            out GlyphOutlineMetrics metrics,
            LoadFlags loadFlags = LoadFlags.NoBitmap | LoadFlags.NoHinting)
        {
            outline = null;
            metrics = default;

            var fontFace = GetOrCreateFontFace(fontFamily, fontStyle);

            lock (freetypeLibrary)
            {
                SetFontFaceSize(fontFace, size);

                return SharpFontOutlineExtractor.TryExtractGlyphOutline(
                    fontFace,
                    (uint)character,
                    out outline,
                    out metrics,
                    loadFlags);
            }
        }

        /// <summary>
        /// Convenience overload when you have a single scalar pixel size.
        /// </summary>
        public bool TryGetGlyphOutline(
            string fontFamily,
            FontStyle fontStyle,
            float size,
            char character,
            out GlyphOutline outline,
            out GlyphOutlineMetrics metrics,
            LoadFlags loadFlags = LoadFlags.NoBitmap | LoadFlags.NoHinting)
        {
            return TryGetGlyphOutline(fontFamily, fontStyle, new Vector2(size, size), character, out outline, out metrics, loadFlags);
        }
    }
}
