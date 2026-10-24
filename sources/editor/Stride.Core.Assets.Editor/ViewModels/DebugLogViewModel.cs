// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// A <see cref="LoggerViewModel"/> that can be displayed as a debug page.
/// </summary>
public sealed class DebugLogViewModel : LoggerViewModel, IDebugPage
{
    public DebugLogViewModel(IViewModelServiceProvider serviceProvider, Logger logger)
        : base(serviceProvider, logger)
    {
    }

    /// <inheritdoc />
    public string Title { get; init; } = string.Empty;
}
