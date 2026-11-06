// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.ViewModel.Progress
{
    /// <summary>
    /// A view model that reports the progress of a long operation.
    /// </summary>
    public interface IProgressViewModel
    {
        double Maximum { get; set; }

        double Minimum { get; set; }

        double ProgressValue { get; set; }

        /// <summary>
        /// Updates the progress message and value. The order of consecutive updates is assured.
        /// </summary>
        void UpdateProgress(string message, double value);
    }
}
