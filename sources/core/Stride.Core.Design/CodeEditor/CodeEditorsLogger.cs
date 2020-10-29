// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Diagnostics;

namespace Stride.Core.CodeEditor
{
    public class CodeEditorsLogger : Logger
    {
        private static CodeEditorsLogger instance;

        public static CodeEditorsLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CodeEditorsLogger();
                    instance.Module = "CodeEditors";
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
