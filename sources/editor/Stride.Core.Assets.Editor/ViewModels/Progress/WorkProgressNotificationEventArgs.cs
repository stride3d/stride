// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;

namespace Stride.Core.Assets.Editor.ViewModels.Progress
{
    public class WorkProgressNotificationEventArgs : EventArgs
    {
        public WorkProgressNotificationEventArgs(WorkProgressViewModel workProgress)
        {
            WorkProgress = workProgress;
        }

        public WorkProgressViewModel WorkProgress { get; private set; }
    }
}
