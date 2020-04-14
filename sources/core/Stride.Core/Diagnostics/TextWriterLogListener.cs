// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A <see cref="LogListener"/> implementation redirecting its output to a <see cref="TextWriter"/>.
    /// </summary>
    public class TextWriterLogListener : LogListener
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextWriterLogListener"/> class.
        /// </summary>
        /// <param name="logStream">The log stream.</param>
        public TextWriterLogListener([NotNull] Stream logStream)
        {
            LogWriter = new StreamWriter(logStream);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextWriterLogListener"/> class.
        /// </summary>
        /// <param name="logWriter">The log writer.</param>
        public TextWriterLogListener(TextWriter logWriter)
        {
            LogWriter = logWriter;
        }

        /// <summary>
        /// Gets the log writer.
        /// </summary>
        /// <value>The log writer.</value>
        public TextWriter LogWriter { get; }

        protected override void OnLog([NotNull] ILogMessage logMessage)
        {
            lock (LogWriter)
            {
                LogWriter.WriteLine(GetDefaultText(logMessage));
                var exceptionMsg = GetExceptionText(logMessage);
                if (!string.IsNullOrEmpty(exceptionMsg))
                {
                    LogWriter.WriteLine(exceptionMsg);
                }
            }
        }

        protected override void Flush()
        {
            if (UseFlushAsync)
            {
                LogWriter.FlushAsync();    
            }
            else
            {
                LogWriter.Flush();
            }
        }
    }
}
