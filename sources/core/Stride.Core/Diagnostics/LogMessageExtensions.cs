// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A set of extensions method to use with the <see cref="LogMessage"/> class.
    /// </summary>
    public static class LogMessageExtensions
    {
        /// <summary>
        /// Gets whether the given log message is a <see cref="LogMessageType.Debug"/> message type
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the given log message is a <see cref="LogMessageType.Debug"/> message, <c>false</c> otherwise.</returns>
        public static bool IsDebug([NotNull] this ILogMessage logMessage)
        {
            return logMessage.Type == LogMessageType.Debug;
        }

        /// <summary>
        /// Gets whether the given log message is a <see cref="LogMessageType.Verbose"/> message type
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the given log message is a <see cref="LogMessageType.Verbose"/> message, <c>false</c> otherwise.</returns>
        public static bool IsVerbose([NotNull] this ILogMessage logMessage)
        {
            return logMessage.Type == LogMessageType.Verbose;
        }

        /// <summary>
        /// Gets whether the given log message is a <see cref="LogMessageType.Info"/> message type
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the given log message is a <see cref="LogMessageType.Info"/> message, <c>false</c> otherwise.</returns>
        public static bool IsInfo([NotNull] this ILogMessage logMessage)
        {
            return logMessage.Type == LogMessageType.Info;
        }

        /// <summary>
        /// Gets whether the given log message is a <see cref="LogMessageType.Warning"/> message type
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the given log message is a <see cref="LogMessageType.Warning"/> message, <c>false</c> otherwise.</returns>
        public static bool IsWarning([NotNull] this ILogMessage logMessage)
        {
            return logMessage.Type == LogMessageType.Warning;
        }

        /// <summary>
        /// Gets whether the given log message is a <see cref="LogMessageType.Error"/> message type
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the given log message is a <see cref="LogMessageType.Error"/> message, <c>false</c> otherwise.</returns>
        public static bool IsError([NotNull] this ILogMessage logMessage)
        {
            return logMessage.Type == LogMessageType.Error;
        }

        /// <summary>
        /// Gets whether the given log message is a <see cref="LogMessageType.Fatal"/> message type
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <returns><c>true</c> if the given log message is a <see cref="LogMessageType.Fatal"/> message, <c>false</c> otherwise.</returns>
        public static bool IsFatal([NotNull] this ILogMessage logMessage)
        {
            return logMessage.Type == LogMessageType.Fatal;
        }

        /// <summary>
        /// Gets whether the given log message is at least as severe as the given severity level.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <param name="minSeverity">The minimal severity level.</param>
        /// <returns><c>true</c> if the given log message is at least as severe as the given severity level, <c>false</c> otherwise.</returns>
        public static bool IsAtLeast([NotNull] this ILogMessage logMessage, LogMessageType minSeverity)
        {
            return logMessage.Type >= minSeverity;
        }

        /// <summary>
        /// Gets whether the given log message is at most as severe as the given severity level.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <param name="maxSeverity">The maximal severity level.</param>
        /// <returns><c>true</c> if the given log message is at most as severe as the given severity level, <c>false</c> otherwise.</returns>
        public static bool IsAtMost([NotNull] this ILogMessage logMessage, LogMessageType maxSeverity)
        {
            return logMessage.Type <= maxSeverity;
        }
    }
}
