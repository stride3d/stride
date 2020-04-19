// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// Configuration for <see cref="GlobalLogger"/>.
    /// </summary>
    public class LoggerConfig
    {
        /// <summary>
        /// Gets or sets the minimum level to allow logging.
        /// </summary>
        /// <value>The level.</value>
        public LogMessageType Level { get; set; }
    }
}
