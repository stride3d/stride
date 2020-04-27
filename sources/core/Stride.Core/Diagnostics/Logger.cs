// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// Base implementation for <see cref="ILogger"/>.
    /// </summary>
    public abstract partial class Logger : ILogger
    {
        private static object _lock = new object();
        protected readonly bool[] EnableTypes;

        /// <summary>
        /// Occurs when a message is logged.
        /// </summary>
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger" /> class.
        /// </summary>
        protected Logger()
        {
            EnableTypes = new bool[(int)LogMessageType.Fatal + 1];
        }

        /// <summary>
        /// Gets the minimum level enabled from the config file.
        /// </summary>
        public static readonly LogMessageType MinimumLevelEnabled = LogMessageType.Info; // AppConfig.GetConfiguration<LoggerConfig>("Logger").Level;

        /// <summary>
        /// True if the debug level is enabled at a global level
        /// </summary>
        public static readonly bool IsDebugEnabled = MinimumLevelEnabled <= LogMessageType.Debug;

        /// <summary>
        /// True if the verbose level is enabled at a global level
        /// </summary>
        public static readonly bool IsVerboseEnabled = MinimumLevelEnabled <= LogMessageType.Verbose;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has errors.
        /// </summary>
        /// <value><c>true</c> if this instance has errors; otherwise, <c>false</c>.</value>
        public bool HasErrors { get; set; }

        /// <summary>
        /// Gets the module name. read-only.
        /// </summary>
        /// <value>The module name.</value>
        public string Module { get; protected internal set; }

        /// <summary>
        /// Activates the log for this logger for a range of <see cref="LogMessageType"/>.
        /// </summary>
        /// <param name="fromLevel">The lowest inclusive level to log for.</param>
        /// <param name="toLevel">The highest inclusive level to log for.</param>
        /// <param name="enabledFlag">if set to <c>true</c> this will enable the log, false otherwise. Default is true.</param>
        /// <remarks>
        /// Outside the specified range the log message type are disabled (!enabledFlag).
        /// </remarks>
        public void ActivateLog(LogMessageType fromLevel, LogMessageType toLevel = LogMessageType.Fatal, bool enabledFlag = true)
        {
            // From lower to higher, so we keep fromLevel < toLevel
            if (fromLevel > toLevel)
            {
                var temp = fromLevel;
                fromLevel = toLevel;
                toLevel = temp;
            }

            for (var i = 0; i < EnableTypes.Length; i++)
                EnableTypes[i] = (i >= (int)fromLevel && i <= (int)toLevel) ? enabledFlag : !enabledFlag;
        }

        /// <summary>
        /// Activates the log for this logger for a specific <see cref="LogMessageType"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="enabledFlag">if set to <c>true</c> [enabled flag].</param>
        /// <remarks>
        /// All other activated type are leaved intact.
        /// </remarks>
        public void ActivateLog(LogMessageType type, bool enabledFlag)
        {
            EnableTypes[(int)type] = enabledFlag;
        }

        /// <summary>
        /// Returns a boolean indicating if a particular <see cref="LogMessageType"/> is activated.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the log is activated, otherwise false.</returns>
        public bool Activated(LogMessageType type)
        {
            return EnableTypes[(int)type];
        }

        public void Log([NotNull] ILogMessage logMessage)
        {
            lock (_lock)
            {
                if (logMessage == null)
                    throw new ArgumentNullException(nameof(logMessage));

                // Even if the type is not enabled, set HasErrors property
                // This allow to know that there is an error even if it is not logger.
                if (logMessage.Type == LogMessageType.Error || logMessage.Type == LogMessageType.Fatal)
                    HasErrors = true;

                // Only log when a particular type is enabled
                if (EnableTypes[(int)logMessage.Type])
                {
                    LogRaw(logMessage);
                    MessageLogged?.Invoke(this, new MessageLoggedEventArgs(logMessage));
                }
            }
        }

        /// <summary>
        /// Internal method used to log a message. All Info, Debug, Error...etc. methods are calling this method.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        protected abstract void LogRaw(ILogMessage logMessage);

        /// <summary>
        /// Extracts the caller info from a variable parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A caller info or null if there is no caller information available.</returns>
        internal static CallerInfo ExtractCallerInfo([NotNull] object[] parameters)
        {
            return (parameters.Length > 0) ? parameters[parameters.Length - 1] as CallerInfo : null;
        }
    }
}
