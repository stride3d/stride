// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Data;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class GameSettingAddConfigurationTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => nameof(GameSettingAddConfigurationTemplateProvider);

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Parent?.Type == typeof(GameSettingsAsset)
               && node.Type == typeof(List<Configuration>) 
               && node.Name == nameof(GameSettingsAsset.Defaults);
    }
}
