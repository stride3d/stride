// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.View
{
    /// <summary>
    /// Interaction logic for PackagePickerWindow.xaml
    /// </summary>
    public partial class PackagePickerWindow : IPackagePickerDialog
    {
        private readonly SessionViewModel session;

        private readonly List<PickablePackageViewModel> selectedPackages = new List<PickablePackageViewModel>();

        public PackagePickerWindow(SessionViewModel session)
        {
            this.session = session;
            InitializeComponent();
            Packages = new List<PickablePackageViewModel>();
            DataContext = this;
            Width = Math.Min(Width, SystemParameters.WorkArea.Width);
            Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        }

        public List<PickablePackageViewModel> Packages { get; set; }

        public bool AllowMultiSelection { get; set; }

        public Func<PickablePackageViewModel, bool> Filter { get; set; }
        
        IReadOnlyCollection<PickablePackageViewModel> IPackagePickerDialog.SelectedPackages => selectedPackages;

        public override async Task<DialogResult> ShowModal()
        {
            foreach (var package in await session.SuggestPackagesToAdd())
            {
                if (Filter == null || Filter(package))
                    Packages.Add(package);
            }

            selectedPackages.Clear();

            await base.ShowModal();

            if (Result == Presentation.Services.DialogResult.Ok)
            {
                selectedPackages.AddRange(PackageListBox.SelectedItems.Cast<PickablePackageViewModel>());
            }
            return Result;
        }
    }
}
