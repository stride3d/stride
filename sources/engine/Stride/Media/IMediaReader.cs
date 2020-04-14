// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Media
{
    public interface IMediaReader
    {
        /// <summary>
        /// Specifies if the extractor has been disposed
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Set the play speed of the media
        /// </summary>
        float SpeedFactor { get; set; }

        /// <summary>
        /// Seek to provided position in the media source.
        /// </summary>
        void Seek(TimeSpan mediaTime);
    }
}
