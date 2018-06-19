
using System;

namespace Xenko.Media
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
