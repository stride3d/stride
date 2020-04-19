// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine
{
    public class BuildStepEventArgs : EventArgs
    {
        public BuildStepEventArgs(BuildStep step, ILogger logger)
        {
            Step = step;
            Logger = logger;
        }

        public BuildStep Step { get; private set; }

        public ILogger Logger { get; set; }
    }
}
