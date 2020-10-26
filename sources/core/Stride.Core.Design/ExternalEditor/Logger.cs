// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Diagnostics;

namespace Stride.Core.CodeEditor
{
    internal class Logger : ILogger
    {
        public string Module { get { return "CodeEditors"; } }

        private static Logger _instance;

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Logger();
                return _instance;
            }
        }
        
        public void Warn(string message, Exception e = null)
        {
            Log(new LogMessage(Module, LogMessageType.Warning, message, e, null));
        }
        
        public void Log(ILogMessage logMessage)
        {
            // not sure, what to do here
        }
    }
}
