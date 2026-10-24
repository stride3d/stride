// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel.Progress;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Shows the progress of background tasks. The editor's dialog service (<c>IEditorDialogService</c>) provides it.
    /// </summary>
    public interface IProgressWindowService
    {
        /// <summary>
        /// Shows a progress window which can provide feedback and log on the progression of a background task. This method is non-blocking.
        /// </summary>
        /// <param name="workProgress">The <see cref="WorkProgressViewModel"/> to use to provide background task information.</param>
        /// <param name="minDelay">The minimal delay to wait before opening the dialog. If the dialog should stay open under certain conditions and the work is finished before this delay, the window will open immediately. Otherwise, it will open only if the work is not finished when the delay is over.</param>
        void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay);
    }
}
