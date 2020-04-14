// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xenko.Core.Annotations;
#if XENKO_PLATFORM_ANDROID
using Android.Util;
#endif
#if XENKO_PLATFORM_WINDOWS_DESKTOP
using Microsoft.Win32.SafeHandles;
#endif

namespace Xenko.Core.Diagnostics
{
    /// <summary>
    /// A <see cref="LogListener"/> implementation redirecting its output to the default OS console. If console is not supported message are output to <see cref="Debug"/>
    /// </summary>
    public class ConsoleLogListener : LogListener
    {
#if XENKO_PLATFORM_WINDOWS_DESKTOP
        private bool isConsoleActive;
#endif

        /// <summary>
        /// Gets or sets the minimum log level handled by this listener.
        /// </summary>
        /// <value>The minimum log level.</value>
        public LogMessageType LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the log mode.
        /// </summary>
        /// <value>The log mode.</value>
        public ConsoleLogMode LogMode { get; set; }

        protected override void OnLog([NotNull] ILogMessage logMessage)
        {
            // filter logs with lower level
            if (!Debugger.IsAttached && // Always log when debugger is attached
                (logMessage.Type < LogLevel || LogMode == ConsoleLogMode.None
                || (!(LogMode == ConsoleLogMode.Auto && Platform.IsRunningDebugAssembly) && LogMode != ConsoleLogMode.Always)))
            {
                return;
            }

            // Make sure the console is opened when the debugger is not attached
            EnsureConsole();

#if XENKO_PLATFORM_ANDROID
            const string appliName = "Xenko";
            var exceptionMsg = GetExceptionText(logMessage);
            var messageText = GetDefaultText(logMessage);
            if (!string.IsNullOrEmpty(exceptionMsg))
                messageText += exceptionMsg;

            // set the color depending on the message log level
            switch (logMessage.Type)
            {
                case LogMessageType.Debug:
                    Log.Debug(appliName, messageText);
                    break;
                case LogMessageType.Verbose:
                    Log.Verbose(appliName, messageText);
                    break;
                case LogMessageType.Info:
                    Log.Info(appliName, messageText);
                    break;
                case LogMessageType.Warning:
                    Log.Warn(appliName, messageText);
                    break;
                case LogMessageType.Error:
                case LogMessageType.Fatal:
                    Log.Error(appliName, messageText);
                    break;
            }
            return;
#else // XENKO_PLATFORM_ANDROID

            var exceptionMsg = GetExceptionText(logMessage);

#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX
            // save initial console color
            var initialColor = Console.ForegroundColor;

            // set the color depending on the message log level
            switch (logMessage.Type)
            {
                case LogMessageType.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case LogMessageType.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogMessageType.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogMessageType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogMessageType.Error:
                case LogMessageType.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
#endif
            
            if (Debugger.IsAttached)
            {
                // Log the actual message
                Debug.WriteLine(GetDefaultText(logMessage));
                if (!string.IsNullOrEmpty(exceptionMsg))
                {
                    Debug.WriteLine(logMessage);
                }
            }

#if !XENKO_PLATFORM_UWP
            // Log the actual message
            Console.WriteLine(GetDefaultText(logMessage));
            if (!string.IsNullOrEmpty(exceptionMsg))
            {
                Console.WriteLine(exceptionMsg);
            }
#endif

#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_PLATFORM_UNIX

            // revert console initial color
            Console.ForegroundColor = initialColor;
#endif
#endif // !XENKO_PLATFORM_ANDROID
        }

#if XENKO_PLATFORM_WINDOWS_DESKTOP

        // TODO: MOVE THIS CODE OUT IN A SEPARATE CLASS

        private void EnsureConsole()
        {
            if (isConsoleActive || !Platform.IsWindowsDesktop)
            {
                return;
            }

            // try to attach to the parent console, if the program is run directly from a console
            var attachedToConsole = AttachConsole(-1);
            if (!attachedToConsole)
            {
                // Else open a new console
                ShowConsole();
            }

            isConsoleActive = true;
        }

        public static void ShowConsole()
        {
            var handle = GetConsoleWindow();

            var outputRedirected = IsHandleRedirected((IntPtr)StdOutConsoleHandle);

            // If we are outputting somewhere unexpected, add an additional console window
            if (outputRedirected)
            {
                var originalStream = Console.OpenStandardOutput();

                // Free before trying to allocate
                FreeConsole();
                AllocConsole();

                var outputStream = Console.OpenStandardOutput();
                if (originalStream != null)
                {
                    outputStream = new DualStream(originalStream, outputStream);
                }

                TextWriter writer = new StreamWriter(outputStream) { AutoFlush = true };
                Console.SetOut(writer);
            }
            else if (handle != IntPtr.Zero)
            {
                const int SW_SHOW = 5;
                ShowWindow(handle, SW_SHOW);
            }
        }

        private class DualStream : Stream
        {
            private readonly Stream stream1;
            private readonly Stream stream2;

            public DualStream(Stream stream1, Stream stream2)
            {
                this.stream1 = stream1;
                this.stream2 = stream2;
            }

            public override void Flush()
            {
                stream1.Flush();
                stream2.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                stream1.Write(buffer, offset, count);
                stream2.Write(buffer, offset, count);
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position { get; set; }
        }

        public static void HideConsole()
        {
            var handle = GetConsoleWindow();
            const int SW_HIDE = 0;
            ShowWindow(handle, SW_HIDE);
        }

        private const int StdOutConsoleHandle = -11;

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(uint nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(uint nStdHandle, IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern int GetFileType(SafeFileHandle handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        private static bool IsHandleRedirected(IntPtr ioHandle)
        {
            if ((GetFileType(new SafeFileHandle(ioHandle, false)) & 2) != 2)
            {
                return true;
            }

            // We are fine with being attached to non-consoles
            return false;

            //int mode;
            //return !GetConsoleMode(ioHandle, out mode);
        }

#else
        private void EnsureConsole()
        {
        }
#endif
    }
}
