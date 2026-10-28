// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModel.Logs;

/// <summary>
/// A source of messages of the asset log (asset builds, thumbnails...) that can be hidden.
/// </summary>
public sealed class AssetLogSourceViewModel : DispatcherViewModel
{
    private readonly Action changed;
    private bool isShown;

    public AssetLogSourceViewModel(IViewModelServiceProvider serviceProvider, string id, string displayName, bool isShown, Action changed)
        : base(serviceProvider)
    {
        Id = id;
        DisplayName = displayName;
        this.isShown = isShown;
        this.changed = changed;
    }

    /// <summary>
    /// Gets the name of the logs of this source (see <see cref="LogKey.Name"/>).
    /// </summary>
    public string Id { get; }

    public string DisplayName { get; }

    public bool IsShown { get => isShown; set => SetValue(ref isShown, value, changed); }
}
