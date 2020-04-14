// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Xenko.ExecServer;

namespace Xenko.Core.Assets.CompilerClient
{
    /// <summary>
    /// Small wrapper to communicate through ExecServer to launch Assets.CompilerApp.exe.
    /// The purpose of this small exe is to have the process name called "CompilerClient" instead
    /// of a generic name "ExecServer".
    /// </summary>
    public class Program
    {
        //[LoaderOptimization(LoaderOptimization.MultiDomain)]
        public static int Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            const string CompilerAppExeName = "Xenko.Core.Assets.CompilerApp.exe";

            var serverApp = new ExecServerApp();
            // The first two parameters are the executable path and the current directory
            var newArgs = new List<string>()
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CompilerAppExeName),
                Environment.CurrentDirectory
            };

            newArgs.AddRange(args);
            var result = serverApp.Run(newArgs.ToArray());

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);

            return result;
        }
    }
}
