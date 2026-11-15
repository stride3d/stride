// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Assets.Editor.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The dialog asking the parameters of a dotnet new template (<see cref="DotNetNewTemplateParametersViewModel"/>): one control per
/// user-facing parameter, and the asset packs when the template offers them. It closes with <c>true</c> when the user accepts.
/// </summary>
public sealed partial class DotNetNewTemplateParametersWindow : Window
{
    public DotNetNewTemplateParametersWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the parameters asked.
    /// </summary>
    public DotNetNewTemplateParametersViewModel? Parameters => DataContext as DotNetNewTemplateParametersViewModel;

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
