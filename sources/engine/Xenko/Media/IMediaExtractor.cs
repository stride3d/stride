
using System;

namespace Xenko.Media
{
    public enum MediaType
    {
        Audio,
        Video
    }

    public interface IMediaExtractor: IMediaReader
    {
        /// <summary>
        /// Returns the total duration of the media
        /// </summary>
        TimeSpan MediaDuration { get; }

        /// <summary>
        /// Gets the current presentation time of the media
        /// </summary>
        TimeSpan MediaCurrentTime { get; }

        /// <summary>
        /// Returns the type of media that is extracted
        /// </summary>
        MediaType MediaType { get; }

        /// <summary>
        /// Specifies if the end of the media has been reached.
        /// </summary>
        /// <returns></returns>
        bool ReachedEndOfMedia();

        /// <summary>
        /// Indicate if a previous seek request has been completed.
        /// </summary>
        /// <returns></returns>
        bool SeekRequestCompleted();
    }
}
