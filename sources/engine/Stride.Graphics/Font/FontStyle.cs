// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// Type of a font.
    /// </summary>
    [Flags]
    [DataContract]
    public enum FontStyle
    {
        /// <summary>
        /// A regular font.
        /// </summary>
        Regular = 0,

        /// <summary>
        /// A bold font.
        /// </summary>
        Bold = 1,

        /// <summary>
        /// An italic font.
        /// </summary>
        Italic = 2,
    }
}
