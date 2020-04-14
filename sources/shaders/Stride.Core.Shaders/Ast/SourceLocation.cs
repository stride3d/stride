// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Stride.Core;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Source location.
    /// </summary>
    [DataContract]
    public struct SourceLocation
    {
        #region Constants and Fields

        /// <summary>
        /// Filename source.
        /// </summary>
        public string FileSource;

        /// <summary>
        /// Absolute position in the file.
        /// </summary>
        public int Position;

        /// <summary>
        /// Line in the file (1-based).
        /// </summary>
        public int Line;

        /// <summary>
        /// Column in the file (1-based).
        /// </summary>
        public int Column;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceLocation"/> struct.
        /// </summary>
        /// <param name="fileSource">The file source.</param>
        /// <param name="position">The position.</param>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        public SourceLocation(string fileSource, int position, int line, int column)
        {
            FileSource = fileSource;
            Position = position;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceLocation"/> struct.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        public SourceLocation(int position, int line, int column)
            : this()
        {
            Position = position;
            Line = line;
            Column = column;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.ToString(false);
        }

        public string ToString(bool useShortFileName)
        {
            return string.Format("{0}({1},{2})", FileSource ?? string.Empty, Line, Column);
        }

        #endregion
    }
}
