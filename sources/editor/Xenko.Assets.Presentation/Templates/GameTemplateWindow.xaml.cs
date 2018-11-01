// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Dialogs;
using Xenko.Core.Presentation.Services;
using Xenko.Graphics;
using Xenko.Core.Presentation.ValueConverters;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Translation;
using Xenko.Assets.Templates;
using MessageBoxButton = Xenko.Core.Presentation.Services.MessageBoxButton;
using MessageBoxImage = Xenko.Core.Presentation.Services.MessageBoxImage;

namespace Xenko.Assets.Presentation.Templates
{
    public class GraphicsProfileAllowsHDR : OneWayValueConverter<GraphicsProfileAllowsHDR>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (GraphicsProfile)value >= GraphicsProfile.Level_10_0;
        }
    }

    /// <summary>
    /// Interaction logic for GameTemplateWindow.xaml
    /// </summary>
    public partial class GameTemplateWindow : INotifyPropertyChanged
    {
        private readonly ViewModelServiceProvider services;
        private GraphicsProfile selectedGraphicsProfile;
        private bool isHDR;

        public GameTemplateWindow(IEnumerable<SolutionPlatform> availablePlatforms, string defaultNamespace)
        {
            var dispatcher = new DispatcherService(Dispatcher);
            var dialog = new DialogService(dispatcher, EditorViewModel.Instance.EditorName);
            services = new ViewModelServiceProvider(new object[] { dispatcher, dialog });
            AvailablePlatforms = availablePlatforms.Select(x => new SolutionPlatformViewModel(services, x, false, x.Type == PlatformType.Windows)).ToList();

            // Obsolete - this will be replaced by AssetPacks at some point
            AssetPackages = new List<AssetPackageViewModel>();
            AssetPackages.Add(new AssetPackageViewModel(services, "Animated Models", new UDirectory("mannequinModel"), false));
            AssetPackages.Add(new AssetPackageViewModel(services, "Building Blocks", new UDirectory("PrototypingBlocks"), false));
            AssetPackages.Add(new AssetPackageViewModel(services, "Materials Pack", new UDirectory("MaterialPackage"), false));
            AssetPackages.Add(new AssetPackageViewModel(services, "Particles Pack", new UDirectory("VFXPackage"), false));
            AssetPackages.Add(new AssetPackageViewModel(services, "Samples Assets", new UDirectory("SamplesAssetPackage"), false));

            Namespace = defaultNamespace;
            Orientation = DisplayOrientation.LandscapeRight;
            InitializeComponent();
            DataContext = this;
            SelectedGraphicsProfile = GraphicsProfile.Level_10_0;
            IsHDR = true;
        }

        public Type OrientationType => typeof(DisplayOrientation);

        public DisplayOrientation Orientation { get; set; }

        public List<SolutionPlatformViewModel> AvailablePlatforms { get; set; }

        public IEnumerable<SelectedSolutionPlatform> SelectedPlatforms { get { return AvailablePlatforms.Where(x => x.IsSelected).Select(x => new SelectedSolutionPlatform(x.SolutionPlatform, x.SelectedTemplate)); } }

        public List<AssetPackageViewModel> AssetPackages { get; set; }

        public IEnumerable<UDirectory> SelectedPackages { get { return AssetPackages.Where(x => x.IsSelected).Select(x => x.PackageLocation); } }

        public GraphicsProfile SelectedGraphicsProfile { get { return selectedGraphicsProfile; } set { selectedGraphicsProfile = value; if (value < GraphicsProfile.Level_10_0) IsHDR = false; OnPropertyChanged(); } }

        public bool IsHDR { get { return isHDR; } set { isHDR = value; OnPropertyChanged(); } }

        public string Namespace { get; set; }

        private async void ButtonOk(object sender, RoutedEventArgs e)
        {
            if (Orientation == DisplayOrientation.Default)
            {
                await services.Get<IDialogService>().MessageBox(Tr._p("Message", "Select an orientation."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!SelectedPlatforms.Any())
            {
                await services.Get<IDialogService>().MessageBox(Tr._p("Message", "Select at least one platform."), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string error;
            if (!NamingHelper.IsValidNamespace(Namespace, out error))
            {
                await services.Get<IDialogService>().MessageBox(string.Format(Tr._p("Message", "Type a valid namespace name. Error with {0}"), error), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Result = Xenko.Core.Presentation.Services.DialogResult.Ok;
            Close();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            Result = Xenko.Core.Presentation.Services.DialogResult.Cancel;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
