// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.Status
{
    public class JobProgressViewModel : DispatcherViewModel
    {
        private readonly string unformattedMessage;
        private int current;
        private int total;
        private bool isIndeterminate;
        private string message;

        public JobProgressViewModel(IViewModelServiceProvider serviceProvider, string message, JobPriority priority, int total)
            : base(serviceProvider)
        {
            unformattedMessage = message;
            Current = 0;
            Message = string.Format(unformattedMessage, 0);
            Priority = priority;
            Total = total;
        }

        public string Message { get { return message; } private set { SetValue(ref message, value); } }

        public JobPriority Priority { get; private set; }

        public int Current { get { return current; } set { SetValue(ref current, value, () => Message = string.Format(unformattedMessage, value)); } }

        public int Total { get { return total; } set { SetValue(ref total, value, () => IsIndeterminate = value < 0); } }

        public bool IsIndeterminate { get { return isIndeterminate; } private set { SetValue(ref isIndeterminate, value); } }
    }
}
