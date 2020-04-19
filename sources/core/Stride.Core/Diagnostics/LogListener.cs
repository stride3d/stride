// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A base class to implement a log listener
    /// </summary>
    public abstract class LogListener : IDisposable
    {
        private int logCountFlushLimit;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogListener"/> class.
        /// </summary>
        protected LogListener()
        {
            LogCountFlushLimit = 1;
            TextFormatter = msg => msg.ToString();
        }

        /// <summary>
        /// Gets the log message count.
        /// </summary>
        /// <value>The log message count.</value>
        public int LogMessageCount { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use flush async].
        /// </summary>
        /// <value><c>true</c> if [use flush async]; otherwise, <c>false</c>.</value>
        public bool UseFlushAsync { get; set; }

        /// <summary>
        /// Gets or sets the function that convert a <see cref="ILogMessage" /> instance into a string.
        /// </summary>
        public Func<ILogMessage, string> TextFormatter { get; set; }

        /// <summary>
        /// Gets or sets the log count flush limit. Default is on every message.
        /// </summary>
        /// <value>The log count flush limit.</value>
        public int LogCountFlushLimit
        {
            get { return logCountFlushLimit; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Value must be > 0");
                }
                
                logCountFlushLimit = value;
            }
        }

        /// <summary>
        /// Called when a log occurred.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        protected abstract void OnLog(ILogMessage logMessage);

        /// <summary>
        /// Returns a boolean indicating whether the log should be flushed. By default, flushing is occurring if the message has a higher level than <see cref="LogMessageType.Info"/>
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the log should be flushed, <c>false</c> otherwise</returns>
        protected virtual bool ShouldFlush([NotNull] ILogMessage logMessage)
        {
            // By default flush if we have more than the level info (Warning, Error, Fatal)
            return (logMessage.Type > LogMessageType.Info) || (LogMessageCount % LogCountFlushLimit) == 0;
        }

        /// <summary>
        /// Flush the log, method to be implemented in a subclass.
        /// </summary>
        protected virtual void Flush()
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Gets the default text for a particular log message.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns>A textual representation of a message.</returns>
        protected virtual string GetDefaultText(ILogMessage logMessage)
        {
            return TextFormatter(logMessage);
        }

        /// <summary>
        /// Gets the text that describes the exception associated to a particular log message.
        /// </summary>
        /// <param name="message">The log message.</param>
        /// <returns>A textual representation of the exception, or <see cref="string.Empty"/> if no exception is associated to this log message.</returns>
        [NotNull]
        protected virtual string GetExceptionText([NotNull] ILogMessage message)
        {
            var serializableLogMessage = message as SerializableLogMessage;
            if (serializableLogMessage != null)
            {
                return serializableLogMessage.ExceptionInfo?.ToString() ?? string.Empty;
            }
            var logMessage = message as LogMessage;
            if (logMessage != null)
            {
                return logMessage.Exception?.ToString() ?? string.Empty;
            }
            throw new ArgumentException("Unsupported log message.");
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="LogListener"/> to <see cref="Action{ILogMessage}"/>.
        /// </summary>
        /// <param name="logListener">The log listener.</param>
        /// <returns>The result of the conversion.</returns>
        [NotNull]
        public static implicit operator Action<ILogMessage>([NotNull] LogListener logListener)
        {
            return logListener.OnLogInternal;
        }

        private void OnLogInternal([NotNull] ILogMessage logMessage)
        {
            OnLog(logMessage);
            LogMessageCount++;
            if (ShouldFlush(logMessage))
            {
                Flush();
            }
        }
    }
}
