// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;

namespace Xenko.Core.Diagnostics
{
    /// <summary>
    /// A base log message used by the logging infrastructure.
    /// </summary>
    /// <remarks>
    /// This class can be derived in order to provide additional custom log information.
    /// </remarks>
    public class LogMessage : ILogMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage" /> class.
        /// </summary>
        public LogMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage" /> class.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="type">The type.</param>
        /// <param name="text">The text.</param>
        public LogMessage(string module, LogMessageType type, string text)
        {
            Module = module;
            Type = type;
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogMessage" /> class.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="type">The type.</param>
        /// <param name="text">The text.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="callerInfo">The caller info.</param>
        public LogMessage(string module, LogMessageType type, string text, Exception exception, CallerInfo callerInfo)
        {
            try {
                // Get stack trace for the exception with source file information
                StackTrace st = new StackTrace(exception, true);
                // Get the top stack frame
                StackFrame frame = st.GetFrame(0);
                // Get the line number from the stack frame
                int line = frame.GetFileLineNumber();
                // try to add it to the front of the text
                text = "{line #" + line.ToString() + "}" + text;
            } catch(Exception e) { /* couldn't get line info */ }

            Module = module;
            Type = type;
            Text = text;
            Exception = exception;
            CallerInfo = callerInfo;
        }

        /// <summary>
        /// Gets or sets the module.
        /// </summary>
        /// <value>The module.</value>
        /// <remarks>
        /// The module is an identifier for a logical part of the system. It can be a class name, a namespace or a regular string not linked to a code hierarchy.
        /// </remarks>
        public string Module { get; set; }

        /// <summary>
        /// Gets or sets the type of this message.
        /// </summary>
        /// <value>The type.</value>
        public LogMessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the caller information.
        /// </summary>
        /// <value>The caller information.</value>
        public CallerInfo CallerInfo { get; set; }

        /// <inheritdoc/>
        public ExceptionInfo ExceptionInfo => Exception != null ? new ExceptionInfo(Exception) : null;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"{(Module != null ? $"[{Module}]: " : string.Empty)}{Type}: {Text}{(Exception != null ? $". {Exception}" : string.Empty)}";
        }
    }
}
