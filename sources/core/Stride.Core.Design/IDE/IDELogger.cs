// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Diagnostics;

namespace Stride.Core.IDE
{
    public class IDELogger : Logger
    {
        private static IDELogger instance;

        public static IDELogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new IDELogger();
                    instance.Module = "IDE";
                    instance.ActivateLog(MinimumLevelEnabled);
                }
                return instance;
            }
        }
        
        protected override void LogRaw(ILogMessage logMessage)
        {
            Log(logMessage);
        }
    }
}
