// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface IProgressViewModel
{
    double Maximum { get; set; }

    double Minimum { get; set; }

    double ProgressValue { get; set; }

    void UpdateProgress(string message, double value);
}
