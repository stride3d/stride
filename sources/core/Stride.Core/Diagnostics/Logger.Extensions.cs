// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// File AUTO-GENERATED, do not edit!
using System;

namespace Stride.Core.Diagnostics
{
    public abstract partial class Logger
    {
        /// <summary>
        /// Logs the specified verbose message with an exception.
        /// </summary>
        /// <param name="message">The verbose message.</param>
        /// <param name="exception">An exception to log with the message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Verbose(string message, Exception exception, CallerInfo callerInfo = null)
        {
            Log(new LogMessage(Module, LogMessageType.Verbose, message, exception, callerInfo));
        }

        /// <summary>
        /// Logs the specified verbose message.
        /// </summary>
        /// <param name="message">The verbose message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Verbose(string message, CallerInfo callerInfo = null)
        {
            Verbose(message, null, callerInfo);
        }
        /// <summary>
        /// Logs the specified debug message with an exception.
        /// </summary>
        /// <param name="message">The debug message.</param>
        /// <param name="exception">An exception to log with the message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Debug(string message, Exception exception, CallerInfo callerInfo = null)
        {
            Log(new LogMessage(Module, LogMessageType.Debug, message, exception, callerInfo));
        }

        /// <summary>
        /// Logs the specified debug message.
        /// </summary>
        /// <param name="message">The debug message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Debug(string message, CallerInfo callerInfo = null)
        {
            Debug(message, null, callerInfo);
        }
        /// <summary>
        /// Logs the specified info message with an exception.
        /// </summary>
        /// <param name="message">The info message.</param>
        /// <param name="exception">An exception to log with the message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Info(string message, Exception exception, CallerInfo callerInfo = null)
        {
            Log(new LogMessage(Module, LogMessageType.Info, message, exception, callerInfo));
        }

        /// <summary>
        /// Logs the specified info message.
        /// </summary>
        /// <param name="message">The info message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Info(string message, CallerInfo callerInfo = null)
        {
            Info(message, null, callerInfo);
        }
        /// <summary>
        /// Logs the specified warning message with an exception.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="exception">An exception to log with the message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Warning(string message, Exception exception, CallerInfo callerInfo = null)
        {
            Log(new LogMessage(Module, LogMessageType.Warning, message, exception, callerInfo));
        }

        /// <summary>
        /// Logs the specified warning message.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Warning(string message, CallerInfo callerInfo = null)
        {
            Warning(message, null, callerInfo);
        }
        /// <summary>
        /// Logs the specified error message with an exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="exception">An exception to log with the message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Error(string message, Exception exception, CallerInfo callerInfo = null)
        {
            Log(new LogMessage(Module, LogMessageType.Error, message, exception, callerInfo));
        }

        /// <summary>
        /// Logs the specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Error(string message, CallerInfo callerInfo = null)
        {
            Error(message, null, callerInfo);
        }
        /// <summary>
        /// Logs the specified fatal message with an exception.
        /// </summary>
        /// <param name="message">The fatal message.</param>
        /// <param name="exception">An exception to log with the message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Fatal(string message, Exception exception, CallerInfo callerInfo = null)
        {
            Log(new LogMessage(Module, LogMessageType.Fatal, message, exception, callerInfo));
        }

        /// <summary>
        /// Logs the specified fatal message.
        /// </summary>
        /// <param name="message">The fatal message.</param>
        /// <param name="callerInfo">Information about the caller. Default is null, otherwise use <see cref="CallerInfo.Get"/>.</param>
        public void Fatal(string message, CallerInfo callerInfo = null)
        {
            Fatal(message, null, callerInfo);
        }
    }
}
