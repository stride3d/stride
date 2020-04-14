// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger.Soft.Xenko
{
    public enum Commands
    {
        StartDebug,
    }

    internal class StartDebugServer : CommandHandler
    {
        protected override void Run()
        {
            IdeApp.ProjectOperations.DebugApplication("XenkoDebugServer", null, null, null);
        }
    }

    internal class StartDebugClient : CommandHandler
    {
        protected override void Run()
        {
            IdeApp.ProjectOperations.DebugApplication("XenkoDebugClient", null, null, null);
        }
    }
}
