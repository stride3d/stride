// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

namespace Stride.Core.Diagnostics;

/// <summary>
/// A logger that redirect messages to a global handler and handle instantiated MapModuleNameToLogger.
/// </summary>
public sealed class GlobalLogger : Logger
{
    #region Constants and Fields

    /// <summary>
    /// Map for all instantiated loggers. Map a module name to a logger.
    /// </summary>
    private static readonly Dictionary<string, Logger> MapModuleNameToLogger = [];

    #endregion

    private GlobalLogger(string module)
    {
        Module = module;
    }

    public delegate void MessageFilterDelegate(ref ILogMessage logMessage);

    /// <summary>
    /// Occurs before a message is logged.
    /// </summary>
    public static event MessageFilterDelegate? GlobalMessageFilter;

    /// <summary>
    /// Occurs when a message is logged.
    /// </summary>
    public static event Action<ILogMessage>? GlobalMessageLogged;

    /// <summary>
    /// Gets all registered loggers.
    /// </summary>
    /// <value>The registered loggers.</value>
    public static Logger[] RegisteredLoggers
    {
        get
        {
            lock (MapModuleNameToLogger)
            {
                var loggers = new Logger[MapModuleNameToLogger.Count];
                MapModuleNameToLogger.Values.CopyTo(loggers, 0);
                return loggers;
            }
        }
    }

    /// <summary>
    /// Activates the log for all loggers using the specified action..
    /// </summary>
    /// <param name="activator">The activator.</param>
    /// <exception cref="ArgumentNullException">If activator is null</exception>
    public static void ActivateLog(Action<Logger> activator)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(activator);
#else
        if (activator is null) throw new ArgumentNullException(nameof(activator));
#endif // NET7_0_OR_GREATER

        foreach (var logger in MapModuleNameToLogger.Values)
            activator(logger);
    }

    /// <summary>
    /// Activates the log for loggers that match a regex pattern on the module name.
    /// </summary>
    /// <param name="regexPatternModule">The regex pattern to match a module name.</param>
    /// <param name="minimumLevel">The minimum level.</param>
    /// <param name="maximumLevel">The maximum level.</param>
    /// <param name="enabledFlag">if set to <c>true</c> enaable the log, else disable.</param>
    /// <exception cref="ArgumentNullException">If regexPatternModule is null</exception>
    public static void ActivateLog(string regexPatternModule, LogMessageType minimumLevel, LogMessageType maximumLevel = LogMessageType.Fatal, bool enabledFlag = true)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(regexPatternModule);
#else
        if (regexPatternModule is null) throw new ArgumentNullException(nameof(regexPatternModule));
#endif // NET7_0_OR_GREATER

        var regex = new Regex(regexPatternModule);
        ActivateLog(regex, minimumLevel, maximumLevel, enabledFlag);
    }

    /// <summary>
    /// Activates the log for loggers that match a regex pattern on the module name.
    /// </summary>
    /// <param name="regexPatternModule">The regex pattern to match a module name.</param>
    /// <param name="minimumLevel">The minimum level.</param>
    /// <param name="maximumLevel">The maximum level.</param>
    /// <param name="enabledFlag">if set to <c>true</c> enaable the log, else disable.</param>
    /// <exception cref="ArgumentNullException">If regexPatternModule is null</exception>
    public static void ActivateLog(Regex regexPatternModule, LogMessageType minimumLevel, LogMessageType maximumLevel = LogMessageType.Fatal, bool enabledFlag = true)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(regexPatternModule);
#else
        if (regexPatternModule is null) throw new ArgumentNullException(nameof(regexPatternModule));
#endif // NET7_0_OR_GREATER

        ActivateLog(
            logger =>
                {
                    if (regexPatternModule.Match(logger.Module ?? string.Empty).Success)
                    {
                        logger.ActivateLog(minimumLevel, maximumLevel, enabledFlag);
                    }
                });
    }

    /// <summary>
    /// Gets the <see cref="GlobalLogger"/> associated to the specified module.
    /// </summary>
    /// <param name="module">The module name.</param>
    /// <exception cref="ArgumentNullException">If module name is null</exception>
    /// <returns>An instance of a <see cref="Logger"/></returns>
    public static Logger GetLogger(string module)
    {
        return GetLogger(module, MinimumLevelEnabled);
    }

    /// <summary>
    /// Gets the <see cref="GlobalLogger"/> associated to the specified module.
    /// </summary>
    /// <param name="module">The module name.</param>
    /// <param name="minimumLevel">Minimum log level (only applied if new logger instance is created)</param>
    /// <exception cref="ArgumentNullException">If module name is null</exception>
    /// <returns>An instance of a <see cref="Logger"/></returns>
    public static Logger GetLogger(string module, LogMessageType minimumLevel)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(module);
#else
        if (module is null) throw new ArgumentNullException(nameof(module));
#endif // NET7_0_OR_GREATER

        Logger? logger;
        lock (MapModuleNameToLogger)
        {
            if (!MapModuleNameToLogger.TryGetValue(module, out logger))
            {
                logger = new GlobalLogger(module);
                logger.ActivateLog(minimumLevel);
                MapModuleNameToLogger.Add(module, logger);
            }
        }
        return logger;
    }

    protected override void LogRaw(ILogMessage logMessage)
    {
        var filterHandler = GlobalMessageFilter;
        if (filterHandler != null)
        {
            filterHandler(ref logMessage);
            if (logMessage == null)
                return;
        }

        GlobalMessageLogged?.Invoke(logMessage);
    }
}
