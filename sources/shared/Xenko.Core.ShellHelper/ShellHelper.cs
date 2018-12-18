// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xenko.Core.Diagnostics;

namespace Xenko
{
    class ShellHelper
    {
        /// <summary>
        /// Run the process and get the output without deadlocks.
        /// </summary>
        /// <param name="command">The name or path of the command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        /// <returns>The outputs.</returns>
        public static Task<int> RunProcessAndGetOutputAsync(string command, string parameters, ILogger logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            var process = new Process
            {
                StartInfo =
                    new ProcessStartInfo(command, parameters)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                    }
            };

            var tcs = new TaskCompletionSource<int>();

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (_, args) => { if (!string.IsNullOrEmpty(args.Data)) { logger.Verbose(args.Data); } };
            process.ErrorDataReceived += (_, args) => { if (!string.IsNullOrEmpty(args.Data)) { logger.Error(args.Data); } };

            process.Exited += (_, args) =>
            {
                tcs.TrySetResult(process.ExitCode);
                process.Dispose();
            };
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);

            if (!process.Start())
                tcs.TrySetException(new InvalidOperationException($"Process [{command}] couldn't start"));

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        /// <summary>
        /// Run the process and get the output without deadlocks.
        /// </summary>
        /// <param name="command">The name or path of the command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        /// <returns>The outputs.</returns>
        public static ProcessOutputs RunProcessAndGetOutput(string command, string parameters)
        {
            var outputs = new ProcessOutputs();
            using (var process = Process.Start(
                new ProcessStartInfo(command, parameters)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                }))
            {
                process.OutputDataReceived += (_, args) => LockProcessAndAddDataToList(process, outputs.OutputLines, args);
                process.ErrorDataReceived += (_, args) => LockProcessAndAddDataToList(process, outputs.OutputErrors, args);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                outputs.ExitCode = process.ExitCode;
            }

            return outputs;
        }

        /// <summary>
        /// Run a process without waiting for its output.
        /// </summary>
        /// <param name="command">The name or path of the command.</param>
        /// <param name="parameters">The parameters of the command.</param>
        public static Process RunProcess(string command, string parameters)
        {
            return Process.Start(
                new ProcessStartInfo(command, parameters)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                });
        }
        public static int RunProcessAndRedirectToLogger(string command, string parameters, string workingDirectory, LoggerResult logger)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(command)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = workingDirectory,
                    Arguments = parameters,
                },
            };

            process.Start();

            DataReceivedEventHandler outputDataReceived = (_, args) => LockProcessAndAddDataToLogger(process, logger, false, args);
            DataReceivedEventHandler errorDataReceived = (_, args) => LockProcessAndAddDataToLogger(process, logger, true, args);

            process.OutputDataReceived += outputDataReceived;
            process.ErrorDataReceived += errorDataReceived;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            process.CancelErrorRead();

            process.OutputDataReceived -= outputDataReceived;
            process.ErrorDataReceived -= errorDataReceived;

            return process.ExitCode;
        }

        /// <summary>
        /// Lock the process and save the string.
        /// </summary>
        /// <param name="process">The current process.</param>
        /// <param name="output">List of saved strings.</param>
        /// <param name="args">arguments of the process.</param>
        private static void LockProcessAndAddDataToList(Process process, List<string> output, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                lock (process)
                {
                    output.Add(args.Data);
                }
            }
        }

        /// <summary>
        /// Lock the process and save the string.
        /// </summary>
        /// <param name="process">The current process.</param>
        /// <param name="logger">Logger were out current process.</param>
        /// <param name="isError">Is this the error output or the standard one?</param>
        /// <param name="args">arguments of the process.</param>
        private static void LockProcessAndAddDataToLogger(Process process, LoggerResult logger, bool isError, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                lock (process)
                {
                    if (isError)
                        logger.Error(args.Data);
                    else
                        logger.Info(args.Data);
                }
            }
        }
    }
}
