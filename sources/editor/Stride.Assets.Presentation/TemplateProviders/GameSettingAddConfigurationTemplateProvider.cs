using System.Collections.Generic;
using Stride.Data;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Presentation.TemplateProviders
{
    /// <summary>
    /// Removes the standard collection header/expander of <see cref="GameSettingsAsset.Defaults"/> and provides a custom footer.
    /// For XAML part see <see href="../View/EntityPropertyTemplates.xaml"/>.
    /// </summary>
    public class GameSettingAddConfigurationTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => nameof(GameSettingAddConfigurationTemplateProvider);

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Parent?.Type == typeof(GameSettingsAsset)
                && node.Type == typeof(List<Configuration>) 
                && node.Name == nameof(GameSettingsAsset.Defaults);
        }
    }
}
