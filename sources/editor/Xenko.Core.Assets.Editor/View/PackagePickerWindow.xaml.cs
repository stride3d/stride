// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Services;

namespace Xenko.Core.Assets.Editor.View
{
    /// <summary>
    /// Interaction logic for PackagePickerWindow.xaml
    /// </summary>
    public partial class PackagePickerWindow : IPackagePickerDialog
    {
        private readonly SessionViewModel session;

        private readonly List<PackageViewModel> selectedPackages = new List<PackageViewModel>();

        public PackagePickerWindow(SessionViewModel session)
        {
            this.session = session;
            InitializeComponent();
            Packages = new List<PackageViewModel>();
            DataContext = this;
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }

        public List<PackageViewModel> Packages { get; set; }

        public bool AllowMultiSelection { get; set; }

        public Func<PackageViewModel, bool> Filter { get; set; }
        
        IReadOnlyCollection<PackageViewModel> IPackagePickerDialog.SelectedPackages => selectedPackages;

        public override async Task<DialogResult> ShowModal()
        {
            foreach (var package in session.LocalPackages)
            {
                if (Filter == null || Filter(package))
                    Packages.Add(package);
            }
            foreach (var package in session.StorePackages)
            {
                if (Filter == null || Filter(package))
                    Packages.Add(package);
            }

            selectedPackages.Clear();

            await base.ShowModal();

            if (Result == Presentation.Services.DialogResult.Ok)
            {
                selectedPackages.AddRange(PackageListBox.SelectedItems.Cast<PackageViewModel>());
            }
            return Result;
        }
    }
}
