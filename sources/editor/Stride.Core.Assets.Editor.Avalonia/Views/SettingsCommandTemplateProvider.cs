// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The button of a <see cref="SettingsCommand"/> in the settings window (e.g. Reset Game Studio layout), as in WPF.
/// </summary>
public sealed class SettingsCommandTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "SettingsCommand";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.NodeValue is SettingsCommand;
    }
}
