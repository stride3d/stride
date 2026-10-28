// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// The main view model of a test host: only the status bar.
/// </summary>
public sealed class FakeMainViewModel : IMainViewModel
{
    public FakeMainViewModel(IViewModelServiceProvider serviceProvider)
    {
        Status = new StatusViewModel(serviceProvider);
    }

    public StatusViewModel Status { get; }
}
