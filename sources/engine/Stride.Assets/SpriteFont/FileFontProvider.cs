// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Core.IO;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont
{
    [DataContract("FileFontProvider")]
    [Display("Font from File")]
    public class FileFontProvider : FontProviderBase
    {
        /// <summary>
        /// Gets or sets the source file containing the font data. This can be a TTF file or a bitmap file.
        /// </summary>
        /// <value>The source.</value>
        /// <userdoc>
        /// The path to the file containing the font data to use.
        /// </userdoc>
        [DataMember(10)]
        [Display("Source")]
        public UFile Source { get; set; } = new UFile("");

        /// <inheritdoc/>
        public override FontFace GetFontFace()
        {
            if (!File.Exists(Source))
            {
                // Font does not exist
                throw new FontNotFoundException(Source);
            }

            var factory = new Factory();

            using (var fontFile = new FontFile(factory, Source))
            {
                FontSimulations fontSimulations;
                switch (Style)
                {
                    case Stride.Graphics.Font.FontStyle.Regular:
                        fontSimulations = FontSimulations.None;
                        break;
                    case Stride.Graphics.Font.FontStyle.Bold:
                        fontSimulations = FontSimulations.Bold;
                        break;
                    case Stride.Graphics.Font.FontStyle.Italic:
                        fontSimulations = FontSimulations.Oblique;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                RawBool isSupported;
                FontFileType fontType;
                FontFaceType faceType;
                int numberFaces;

                fontFile.Analyze(out isSupported, out fontType, out faceType, out numberFaces);

                return new FontFace(factory, faceType, new[] { fontFile }, 0, fontSimulations);
            }
        }

        /// <inheritdoc/>
        public override string GetFontPath(AssetCompilerResult result = null)
        {
            if (!File.Exists(Source))
            {
                result?.Error($"Cannot find font file '{Source}'. Make sure it exists and is referenced correctly.");
            }
            return Source;
        }

        /// <inheritdoc/>
        public override string GetFontName()
        {
            return Source.GetFileNameWithoutExtension();
        }
    }
}
