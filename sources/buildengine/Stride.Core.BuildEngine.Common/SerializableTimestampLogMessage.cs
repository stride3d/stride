// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine
{
    /// <summary>
    /// A specialization of the <see cref="SerializableLogMessage"/> class that contains a timestamp information.
    /// </summary>
    [DataContract]
    public class SerializableTimestampLogMessage : SerializableLogMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableTimestampLogMessage"/> class with default values for its properties
        /// </summary>
        public SerializableTimestampLogMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableTimestampLogMessage"/> class from a <see cref="TimestampLocalLogger.Message"/> instance.
        /// </summary>
        /// <param name="message">The <see cref="TimestampLocalLogger.Message"/> instance to use to initialize properties.</param>
        public SerializableTimestampLogMessage(TimestampLocalLogger.Message message)
            : base((LogMessage)message.LogMessage)
        {
            Timestamp = message.Timestamp;
        }

        /// <summary>
        /// Gets or sets the timestamp of this message.
        /// </summary>
        public long Timestamp { get; set; }
    }
}
