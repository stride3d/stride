// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Text;
using Xenko.Core.Annotations;

namespace Xenko.Core.Extensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Represents the maximum number of lines to include in the stack trace when formatting a exception to be displayed in a dialog.
        /// </summary>
        public const int MaxStackTraceLines = 30;

        /// <summary>
        /// Explicitly ignores the exception. This method does nothing but suppress warnings related to a catch block doing nothing.
        /// </summary>
        /// <param name="exception">The exception to ignore.</param>
        public static void Ignore([NotNull] this Exception exception)
        {
            // Intentionally does nothing.
        }

        /// <summary>
        /// Formats the exception to be displayed in a dialog message. This methods will limit the number of lines to the value of <see cref="MaxStackTraceLines"/>.
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <param name="startWithNewLine">Indicate whether a <see cref="Environment.NewLine"/> symbol should be included at the beginning of the resulting string.</param>
        /// <returns>A string representing the exception formatted for dialog message.</returns>
        [NotNull]
        public static string FormatSummary([NotNull] this Exception exception, bool startWithNewLine)
        {
            // Get the innermost exception.
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }
            var stackTrace = ExtractStackTrace(exception, 0, MaxStackTraceLines);
            return $"{(startWithNewLine ? Environment.NewLine : "")}{exception.Message}{Environment.NewLine}{stackTrace}";
        }

        /// <summary>
        /// Formats the exception to be displayed in a log or report. This method will process <see cref="AggregateException"/>,
        /// expand <see cref="Exception.InnerException"/>, and does not limit the number of line of the resulting string.
        /// </summary>
        /// <param name="exception">The exception to format</param>
        /// <param name="indentIncrement">The number of spaces to add to the current indent when printing an inner exception.</param>
        /// <param name="indent">The number of spaces to insert at the beginning of each line.</param>
        /// <returns>A string representing the exception formatted for log or report.</returns>
        [NotNull]
        public static string FormatFull([NotNull] this Exception exception, int indentIncrement = 2, int indent = 0)
        {
            var stringBuilder = new StringBuilder();
            FormatForReportRecursively(stringBuilder, exception, indentIncrement, indent);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Extracts the stack trace from an exception, formatting it correctly and limiting the number of lines if needed.
        /// </summary>
        /// <param name="exception">The exception from which to extract the stack trace.</param>
        /// <param name="indent">The number of spaces to insert at the beginning of each line.</param>
        /// <param name="maxLines">The maximum number of lines to return in the resulting string. Zero or negative numbers mean no limit.</param>
        /// <returns>A properly formated string containing the stack trace.</returns>
        [NotNull]
        public static string ExtractStackTrace([NotNull] this Exception exception, int indent = 0, int maxLines = -1)
        {
            var sb = new StringBuilder();
            ExtractStackTrace(sb, exception, indent, maxLines);
            return sb.ToString();
        }

        private static void FormatForReportRecursively([NotNull] StringBuilder sb, [NotNull] Exception exception, int indentIncrement, int indent)
        {
            var indentString = string.Empty.PadLeft(indent);
            sb.Append(indentString);
            sb.Append(exception.GetType().Name);
            sb.Append(": ");
            sb.AppendLine(exception.Message);
            ExtractStackTrace(sb, exception, indent, -1);

            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                sb.AppendLine();
                sb.Append(indentString);
                sb.AppendLine("AggregateException - InnerExceptions:");
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    FormatForReportRecursively(sb, innerException, indentIncrement, indent + indentIncrement);
                }
            }

            if (exception.InnerException != null)
            {
                sb.AppendLine();
                sb.Append(indentString);
                sb.AppendLine("InnerException:");
                FormatForReportRecursively(sb, exception.InnerException, indentIncrement, indent + indentIncrement);
            }
        }

        private static void ExtractStackTrace([NotNull] StringBuilder sb, [NotNull] Exception exception, int indent, int maxLines)
        {
            if (exception.StackTrace == null)
                return;

            var indentString = "".PadLeft(indent);
            var stackTraceArray = exception.StackTrace.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in maxLines > 0 ? stackTraceArray.Take(maxLines) : stackTraceArray)
            {
                sb.Append(indentString);
                sb.AppendLine(line);
            }
        }
    }
}
