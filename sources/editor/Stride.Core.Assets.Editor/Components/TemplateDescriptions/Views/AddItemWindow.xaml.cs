// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Input;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views
{
    /// <summary>
    /// Interaction logic for AddAssetWindow.xaml
    /// </summary>
    public partial class AddItemWindow : IItemTemplateDialog
    {
        private bool validated;

        public AddItemWindow(IViewModelServiceProvider serviceProvider, TemplateDescriptionCollectionViewModel templateDescriptions)
        {
            DataContext = templateDescriptions;
            InitializeComponent();
            AddItemCommand = new AnonymousCommand<ITemplateDescriptionViewModel>(serviceProvider, ValidateSelectedTemplate);
        }

        public ICommand AddItemCommand { get; }

        public ITemplateDescriptionViewModel SelectedTemplate { get; private set; }

        private void ValidateSelectedTemplate(ITemplateDescriptionViewModel template)
        {
            // Prevent re-entrancy
            if (validated)
                return;

            validated = true;
            SelectedTemplate = template;
            Result = SelectedTemplate != null ? Presentation.Services.DialogResult.Ok : Presentation.Services.DialogResult.Cancel;
            Close();
        }
    }
}
