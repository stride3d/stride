// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Diagnostics;

namespace Stride.Core.IDE
{
    public class IDELogger : TimestampLocalLogger
    {
        private static IDELogger instance;

        public static IDELogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new IDELogger(DateTime.Now, "IDE");
                    instance.ActivateLog(MinimumLevelEnabled);
                }
                return instance;
            }
        }

        public IDELogger(DateTime startTime, string moduleName = null) : base(startTime, moduleName)
        {
        }
    }
}
