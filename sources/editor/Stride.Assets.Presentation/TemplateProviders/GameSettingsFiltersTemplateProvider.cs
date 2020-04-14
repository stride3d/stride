// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class GameSettingsFiltersTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => nameof(GameSettingsFiltersTemplateProvider);

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Name == "SpecificFilter" && node.Root.Type == typeof(GameSettingsAsset);
        }
    }
}
