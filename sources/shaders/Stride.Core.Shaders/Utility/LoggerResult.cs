// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Text;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Utility
{
    /// <summary>
    /// A class to collect parsing/expression messages.
    /// </summary>
    public class LoggerResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerResult"/> class. 
        /// </summary>
        public LoggerResult()
        {
            this.Messages = new List<ReportMessage>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has errors.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has errors; otherwise, <c>false</c>.
        /// </value>
        public bool HasErrors { get; set; }

        /// <summary>
        /// Gets or sets the messages.
        /// </summary>
        /// <value>
        /// The messages.
        /// </value>
        public IList<ReportMessage> Messages { get; private set; }

        /// <summary>
        /// Dumps the messages.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="writer">The writer.</param>
        public void DumpMessages(ReportMessageLevel level, TextWriter writer)
        {
            foreach (var reportMessage in this.Messages)
            {
                if (reportMessage.Level >= level)
                {
                    writer.WriteLine(reportMessage);
                }
            }
        }

        /// <summary>
        /// Copies all messages to another instance.
        /// </summary>
        /// <param name="results">The results.</param>
        public void CopyTo(LoggerResult results)
        {
            foreach (var reportMessage in this.Messages)
            {
                results.Messages.Add(reportMessage);
            }

            if (HasErrors)
                results.HasErrors = true;
        }

        /// <summary>
        /// Logs an Error with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        public void Error(MessageCode message, SourceSpan span)
        {
            this.AddMessage(ReportMessageLevel.Error, message, span);
        }

        /// <summary>
        /// Logs an Error with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        /// <param name="parameters">The parameters.</param>
        public void Error(MessageCode message, SourceSpan span, params object[] parameters)
        {
            this.AddMessage(ReportMessageLevel.Error, message, span, parameters);
        }

        /// <summary>
        /// Logs an Info with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        public void Info(MessageCode message, SourceSpan span)
        {
            this.AddMessage(ReportMessageLevel.Info, message, span);
        }

        /// <summary>
        /// Logs an Info with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        /// <param name="parameters">The parameters.</param>
        public void Info(MessageCode message, SourceSpan span, params object[] parameters)
        {
            this.AddMessage(ReportMessageLevel.Info, message, span, parameters);
        }

        /// <summary>
        /// Logs an Warning with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        public void Warning(MessageCode message, SourceSpan span)
        {
            this.AddMessage(ReportMessageLevel.Warning, message, span);
        }

        /// <summary>
        /// Logs an Warning with the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        /// <param name="parameters">The parameters.</param>
        public void Warning(MessageCode message, SourceSpan span, params object[] parameters)
        {
            this.AddMessage(ReportMessageLevel.Warning, message, span, parameters);
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="level">The type.</param>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        protected void AddMessage(ReportMessageLevel level, MessageCode message, SourceSpan span)
        {
            if (level == ReportMessageLevel.Error) this.HasErrors = true;
            this.Messages.Add(new ReportMessage(level, message.Code, message.Text, span));
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="level">The type.</param>
        /// <param name="message">The message.</param>
        /// <param name="span">The span.</param>
        /// <param name="parameters">The parameters.</param>
        protected void AddMessage(ReportMessageLevel level, MessageCode message, SourceSpan span, params object[] parameters)
        {
            if (level == ReportMessageLevel.Error) this.HasErrors = true;
            this.Messages.Add(new ReportMessage(level, message.Code, string.Format(message.Text, parameters), span));
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            if (HasErrors)
            {
                foreach (var reportMessage in Messages)
                {
                    text.AppendLine(reportMessage.ToString());
                }
            }
            else
            {
                text.AppendLine("OK");
            }
            return text.ToString();
        }
    }
}
