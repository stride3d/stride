// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Assets.Editor.ViewModel.Progress
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
