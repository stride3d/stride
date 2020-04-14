// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.ExecServer
{
    internal class Program
    {
        /// <summary>
        /// Main entry point for ExecServer. Add an attribute to notify that the server is hosting multiple domains using same assemblies.
        /// </summary>
        /// <param name="args">Program arguments</param>
        /// <returns>Status</returns>
        //[LoaderOptimization(LoaderOptimization.MultiDomain)]
        public static int Main(string[] args)
        {
            var serverApp = new ExecServerApp();
            return serverApp.Run(args);
        }
    }
}
