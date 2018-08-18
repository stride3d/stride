// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using NuGet.Common;
using ILogger = NuGet.Common.ILogger;

namespace Xenko.Core.Packages
{
    /// <summary>
    /// Implementation of the <see cref="ILogger"/> interface using our <see cref="IPackagesLogger"/> interface.
    /// </summary>
    internal class NugetLogger : ILogger
    {
        private readonly IPackagesLogger logger;

        /// <summary>
        /// Initialize new instance of NugetLogger.
        /// </summary>
        /// <param name="logger">The <see cref="IPackagesLogger"/> instance to use to implement <see cref="ILogger"/></param>
        public NugetLogger(IPackagesLogger logger)
        {
            this.logger = logger;
        }

        #region ILogger implementation

        /// <summary>
        /// Logs a debug message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogDebug(string data)
        {
            logger.Log(MessageLevel.Debug, data);
        }

        /// <summary>
        /// Logs a verbose message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogVerbose(string data)
        {
            logger.Log(MessageLevel.Verbose, data);
        }

        /// <summary>
        /// Logs an information message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogInformation(string data)
        {
            logger.Log(MessageLevel.Info, data);
        }

        /// <summary>
        /// Logs a minimal message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogMinimal(string data)
        {
            logger.Log(MessageLevel.Minimal, data);
        }

        /// <summary>
        /// Logs a warning message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogWarning(string data)
        {
            logger.Log(MessageLevel.Warning, data);
        }

        /// <summary>
        /// Logs an error message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogError(string data)
        {
            logger.Log(MessageLevel.Error, data);
        }

        /// <summary>
        /// Logs an information summary message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogInformationSummary(string data)
        {
            logger.Log(MessageLevel.InfoSummary, data);
        }

        /// <summary>
        /// Logs an error summary message <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The message to log.</param>
        public void LogErrorSummary(string data)
        {
            logger.Log(MessageLevel.ErrorSummary, data);
        }

        /// <summary>
        /// Logs a message <paramref name="data"/> using the log <paramref name="level"/>.
        /// </summary>
        /// <param name="level">The level of the logged message.</param>
        /// <param name="data">The message to log.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is not a valid log level.</exception>
        public void Log(LogLevel level, string data)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    LogDebug(data);
                    break;
                case LogLevel.Verbose:
                    LogVerbose(data);
                    break;
                case LogLevel.Information:
                    LogInformation(data);
                    break;
                case LogLevel.Minimal:
                    LogMinimal(data);
                    break;
                case LogLevel.Warning:
                    LogWarning(data);
                    break;
                case LogLevel.Error:
                    LogError(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        /// <summary>
        /// Logs a message <paramref name="data"/> using the log <paramref name="level"/>.
        /// </summary>
        /// <param name="level">The level of the logged message.</param>
        /// <param name="data">The message to log.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is not a valid log level.</exception>
        public Task LogAsync(LogLevel level, string data)
        {
            switch (level)
            {
            case LogLevel.Debug:
                return logger.LogAsync(MessageLevel.Debug, data);
            case LogLevel.Verbose:
                return logger.LogAsync(MessageLevel.Verbose, data);
            case LogLevel.Information:
                return logger.LogAsync(MessageLevel.Info, data);
            case LogLevel.Minimal:
                return logger.LogAsync(MessageLevel.Minimal, data);
            case LogLevel.Warning:
                return logger.LogAsync(MessageLevel.Warning, data);
            case LogLevel.Error:
                return logger.LogAsync(MessageLevel.Error, data);
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        /// <summary>
        /// Logs a message <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        /// <summary>
        /// Logs a message <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public Task LogAsync(ILogMessage message)
        {
            return LogAsync(message.Level, message.Message);
        }

        #endregion
    }
}
