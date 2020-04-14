// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Presentation.Dialogs;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.View;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using Stride.Assets.Templates;
using MessageBoxButton = Stride.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Stride.Core.Presentation.Services.MessageBoxImage;
using MessageBoxResult = Stride.Core.Presentation.Services.MessageBoxResult;

namespace Stride.Assets.Presentation.Templates
{

    /// <summary>
    /// Interaction logic for GameTemplateWindow.xaml
    /// </summary>
    public partial class UpdatePlatformsWindow
    {
        private readonly ViewModelServiceProvider services;

        public UpdatePlatformsWindow(ICollection<PlatformType> installedPlatforms)
        {
            if (installedPlatforms == null) throw new ArgumentNullException(nameof(installedPlatforms));
            var dispatcher = new DispatcherService(Dispatcher);
            var dialog = new DialogService(dispatcher, EditorViewModel.Instance.EditorName);
            services = new ViewModelServiceProvider(new object[] { dispatcher, dialog });
            AvailablePlatforms = new List<SolutionPlatformViewModel>();
            foreach (var platform in AssetRegistry.SupportedPlatforms)
            {
                var isInstalled = installedPlatforms.Contains(platform.Type);
                var solutionPlatform = new SolutionPlatformViewModel(services, platform, isInstalled, isInstalled);
                AvailablePlatforms.Add(solutionPlatform);
            }
            ForcePlatformRegenerationVisible = true;
            InitializeComponent();
            DataContext = this;
        }

        public List<SolutionPlatformViewModel> AvailablePlatforms { get; }

        public bool ForcePlatformRegenerationVisible { get; set; }

        public bool ForcePlatformRegeneration { get; set; }

        public IEnumerable<SelectedSolutionPlatform> SelectedPlatforms { get { return AvailablePlatforms.Where(x => x.IsSelected).Select(x => new SelectedSolutionPlatform(x.SolutionPlatform, x.SelectedTemplate)); } }

        private async void ButtonOk(object sender, RoutedEventArgs e)
        {
            if (!SelectedPlatforms.Any())
            {
                await services.Get<IDialogService>().MessageBox(Tr._p("Message", "You must select at least one platform."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (AvailablePlatforms.Any(x => x.MarkedToRemove))
            {
                var buttons = DialogHelper.CreateButtons(new[]
                {
                    Tr._p("Button", "Remove"),
                    Tr._p("Button", "Cancel")
                }, 1, 2);
                var msg = string.Format(Tr._p("Message", "Are you sure you want to remove these {0} platform(s) from the package?"), AvailablePlatforms.Count(x => x.MarkedToRemove));
                var result = await services.Get<IDialogService>().MessageBox(msg, buttons, MessageBoxImage.Question);
                if (result != 1)
                    return;

                Result = Stride.Core.Presentation.Services.DialogResult.Ok;
                Close();
                return;
            }

            Result = Stride.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Result = Stride.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }
    }
}
