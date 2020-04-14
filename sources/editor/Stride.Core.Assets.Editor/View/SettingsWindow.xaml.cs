// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Settings.ViewModels;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.View
{
    public class SettingsTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "SettingsEntry";

        public override bool MatchNode(NodeViewModel node)
        {
            return node?.NodeValue is PackageSettingsWrapper.SettingsKeyWrapper;
        }
    }

    public class SettingsStringFromAcceptableValuesTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "StringFromAcceptableValues";

        public override bool MatchNode(NodeViewModel node)
        {
            object hasAcceptableValues;
            return node.Parent != null && (node.Parent.AssociatedData.TryGetValue("HasAcceptableValues", out hasAcceptableValues) && (bool)hasAcceptableValues);
        }
    }

    public class SettingsCommandTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "SettingsCommand";

        public override bool MatchNode(NodeViewModel node)
        {
            return node?.NodeValue is SettingsCommand;
        }
    }

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow(IViewModelServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(serviceProvider, EditorSettings.SettingsContainer.CurrentProfile);
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }
    }
}
