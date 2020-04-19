// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Streaming
{
    /// <summary>
    /// Available options when streaming a resource.
    /// </summary>
    public struct StreamingOptions
    {
        /// <summary>
        /// The default steaming options: KeepLoaded=false, ForceHighestQuality=false, LoadImmediately=false
        /// </summary>
        public static StreamingOptions Default = new StreamingOptions();

        /// <summary>
        /// Request the immediate loading of the resource to its highest level of quality.
        /// </summary>
        public static StreamingOptions LoadAtOnce = new StreamingOptions { LoadImmediately = true };

        /// <summary>
        /// Do not stream the texture always keep it to the higest quality
        /// </summary>
        public static StreamingOptions DoNotStream = new StreamingOptions { LoadImmediately = true, IgnoreResource = true };

        /// <summary>
        /// Keep the resource loaded even if not rendered on the screen.
        /// </summary>
        public bool KeepLoaded;

        /// <summary>
        /// Force the resource to be loaded at highest quality.
        /// </summary>
        public bool ForceHighestQuality;

        /// <summary>
        /// Block the execution flow and load the resource up to maximum quality synchronously.
        /// </summary>
        public bool LoadImmediately;

        /// <summary>
        /// Do not update the resource data
        /// </summary>
        public bool IgnoreResource;

        /// <summary>
        /// Merge and return the combination of two streaming options for a same resource.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public StreamingOptions CombineWith(StreamingOptions other)
        {
            return new StreamingOptions
            {
                KeepLoaded = other.KeepLoaded || KeepLoaded,
                ForceHighestQuality = other.ForceHighestQuality || ForceHighestQuality,
                LoadImmediately = other.LoadImmediately || LoadImmediately,
                IgnoreResource = other.IgnoreResource || IgnoreResource,
            };
        }
    }
}
