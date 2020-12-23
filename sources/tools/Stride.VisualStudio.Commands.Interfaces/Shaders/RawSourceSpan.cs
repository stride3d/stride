// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.VisualStudio.Commands.Shaders
{
    /// <summary>
    /// Defines a Span span in a file.
    /// </summary>
    [Serializable]
    public class RawSourceSpan
    {
        public RawSourceSpan()
        {
        }

        public RawSourceSpan(string file, int line, int column)
        {
            File = file;
            Line = line;
            Column = column;
            EndLine = line;
            EndColumn = column;
        }

        public string File { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public int EndLine { get; set; }

        public int EndColumn { get; set; }

        public override string ToString()
        {
            // TODO: include span
            return string.Format("{0}({1},{2})", File ?? string.Empty, Line, Column);
        }
    }
}
