// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// A base for the view models of panels.
/// </summary>
public abstract class PanelViewModel : ViewModelBase, IPanelViewModel
{
    private string title;
    private bool isShown;

    /// <summary>
    /// Creates the view model of a panel, titled as its descriptor says.
    /// </summary>
    protected PanelViewModel(PanelContext context)
        : base(context.ServiceProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        PanelId = context.Descriptor.Id;
        InstanceId = context.InstanceId;
        title = context.Descriptor.Title();
    }

    /// <inheritdoc />
    public string PanelId { get; }

    /// <inheritdoc />
    public string InstanceId { get; }

    /// <inheritdoc />
    public string Title { get => title; protected set => SetValue(ref title, value); }

    /// <inheritdoc />
    public bool IsShown { get => isShown; set => SetValue(ref isShown, value, OnIsShownChanged); }

    /// <inheritdoc />
    public virtual Task<bool> CanCloseAsync() => Task.FromResult(true);

    /// <summary>
    /// Called when <see cref="IsShown"/> changes.
    /// </summary>
    protected virtual void OnIsShownChanged()
    {
    }
}
