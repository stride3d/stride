// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger.Soft.Stride
{
    public enum Commands
    {
        StartDebug,
    }

    internal class StartDebugServer : CommandHandler
    {
        protected override void Run()
        {
            IdeApp.ProjectOperations.DebugApplication("StrideDebugServer", null, null, null);
        }
    }

    internal class StartDebugClient : CommandHandler
    {
        protected override void Run()
        {
            IdeApp.ProjectOperations.DebugApplication("StrideDebugClient", null, null, null);
        }
    }
}
