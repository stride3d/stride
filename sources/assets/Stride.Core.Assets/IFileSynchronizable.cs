// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Identify an object that is associated with an anchor file on the disk where all the <see cref="UPath"/> members of this 
    /// instance are relative to the <see cref="FullPath"/> of this instance.
    /// </summary>
    public interface IFileSynchronizable
    {
        /// <summary>
        /// Gets the full path on disk where this instance is stored.
        /// </summary>
        /// <value>The full path.</value>
        UFile FullPath { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is dirty.
        /// </summary>
        /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
        bool IsDirty { get; set; }
    }
}
