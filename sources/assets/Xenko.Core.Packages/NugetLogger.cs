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

        public async Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
        }

        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        public async Task LogAsync(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        #endregion
    }
}
