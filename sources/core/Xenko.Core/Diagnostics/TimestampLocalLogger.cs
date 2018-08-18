// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Xenko.Core.Diagnostics
{
    /// <summary>
    /// A logger that stores messages locally with their timestamp, useful for internal log scenarios.
    /// </summary>
    public class TimestampLocalLogger : Logger
    {
        private readonly DateTime startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampLocalLogger"/> class.
        /// </summary>
        public TimestampLocalLogger(DateTime startTime, string moduleName = null)
        {
            this.startTime = startTime;

            Module = moduleName;
            Messages = new List<Message>();

            // By default, all logs are enabled for a local logger.
            ActivateLog(LogMessageType.Verbose);
        }

        /// <summary>
        /// Gets the messages logged to this instance.
        /// </summary>
        /// <value>The messages.</value>
        public List<Message> Messages { get; }

        protected override void LogRaw(ILogMessage logMessage)
        {
            var timestamp = DateTime.Now - startTime;
            lock (Messages)
            {
                Messages.Add(new Message(timestamp.Ticks, logMessage));
            }
        }

        /// <summary>
        /// A structure describing a log message associated with a timestamp.
        /// </summary>
        public struct Message
        {
            /// <summary>
            /// The timestamp associated to the log message.
            /// </summary>
            public long Timestamp;

            /// <summary>
            /// The log message.
            /// </summary>
            public ILogMessage LogMessage;

            /// <summary>
            /// Initializes a new instance of the <see cref="Message"/> struct.
            /// </summary>
            /// <param name="timestamp">The timestamp associated to the log message.</param>
            /// <param name="logMessage">The log message.</param>
            public Message(long timestamp, ILogMessage logMessage)
            {
                Timestamp = timestamp;
                LogMessage = logMessage;
            }
        }
    }
}
