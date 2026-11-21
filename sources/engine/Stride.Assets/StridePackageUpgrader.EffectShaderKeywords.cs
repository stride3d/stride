// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Text;

namespace Stride.Assets
{
    public partial class StridePackageUpgrader
    {
        /// <summary>
        /// The text of an .sdfx file with <c>effect</c> in place of the <c>shader</c> keyword of each effect declared with it, or
        /// <c>null</c> when it has none or doesn't parse (its build then reports the error).
        /// </summary>
        internal static string UpgradeEffectShaderKeywords(string text, out int count)
        {
            count = 0;
            var parsed = Stride.Shaders.Parsing.SDSLParser.Parse(text, isEffectFile: true);
            if (parsed.Errors.Count > 0 || parsed.AST is not Stride.Shaders.Parsing.ShaderFile file)
                return null;

            // The parser reads a copy of the text with its comments blanked, so its offsets are those of the text
            var offsets = file.RootDeclarations.Concat(file.Namespaces.SelectMany(x => x.Declarations))
                .OfType<Stride.Shaders.Parsing.SDFX.AST.ShaderEffect>()
                .Where(x => x.ShaderKeyword != null)
                .Select(x => x.ShaderKeyword.Value.Range.GetOffsetAndLength(text.Length).Offset)
                .ToList();
            if (offsets.Count == 0 || offsets.Any(x => !text.AsSpan(x).StartsWith("shader", StringComparison.Ordinal)))
                return null;

            var upgraded = new StringBuilder(text);
            foreach (var offset in offsets)
                upgraded.Remove(offset, "shader".Length).Insert(offset, "effect");
            count = offsets.Count;
            return upgraded.ToString();
        }
    }
}
