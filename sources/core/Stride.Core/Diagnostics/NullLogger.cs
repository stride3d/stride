// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.Diagnostics
{
    internal class NullLogger : Logger
    {
        protected override void LogRaw(ILogMessage logMessage)
        {
            // Discard the message
        }
    }
}
