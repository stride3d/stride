// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Utility
{
    public class SpanConverter
    {
        public static SourceLocation Convert(Irony.Parsing.SourceLocation sourceLocation)
        {
            return new SourceLocation(sourceLocation.SourceFilename, sourceLocation.Position, sourceLocation.Line, sourceLocation.Column);
        }

        public static Irony.Parsing.SourceLocation Convert(SourceLocation sourceLocation)
        {
            return new Irony.Parsing.SourceLocation(sourceLocation.Position, sourceLocation.Line, sourceLocation.Column, sourceLocation.FileSource);
        }

        public static SourceSpan Convert(Irony.Parsing.SourceSpan sourceSpan)
        {
            return new SourceSpan(Convert(sourceSpan.Location), sourceSpan.Length);
        }
    }
}
