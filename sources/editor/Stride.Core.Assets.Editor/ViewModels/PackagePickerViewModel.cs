// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The package picker of the Add dependency command: the packages that pass the caller's filter, narrowed to the ones whose name
/// contains a search text. One package is picked, as in the WPF picker.
/// </summary>
public sealed class PackagePickerViewModel : DispatcherViewModel
{
    private readonly IReadOnlyList<PickablePackageViewModel> allPackages;
    private IReadOnlyList<PickablePackageViewModel> packages;
    private string searchText = string.Empty;
    private PickablePackageViewModel? selectedPackage;

    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="candidates">The packages that can be picked (<see cref="SessionViewModel.SuggestPackagesToAddAsync"/>).</param>
    /// <param name="filter">The condition a candidate must meet to be listed, or <c>null</c>.</param>
    public PackagePickerViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<PickablePackageViewModel> candidates, Func<PickablePackageViewModel, bool>? filter = null)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        allPackages = candidates.Where(x => filter is null || filter(x)).ToList();
        packages = allPackages;
    }

    /// <summary>
    /// Gets the listed packages: the ones that pass the filter and whose name contains <see cref="SearchText"/>.
    /// </summary>
    public IReadOnlyList<PickablePackageViewModel> Packages
    {
        get => packages;
        private set => SetValue(ref packages, value);
    }

    /// <summary>
    /// Gets or sets the text the names of the listed packages contain (case-insensitive); empty lists them all.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (!SetValue(ref searchText, value ?? string.Empty))
                return;
            Packages = string.IsNullOrWhiteSpace(searchText)
                ? allPackages
                : allPackages.Where(x => x.Name.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            if (selectedPackage is not null && !Packages.Contains(selectedPackage))
                SelectedPackage = null;
        }
    }

    /// <summary>
    /// Gets or sets the picked package, or <c>null</c>.
    /// </summary>
    public PickablePackageViewModel? SelectedPackage
    {
        get => selectedPackage;
        set => SetValue(ref selectedPackage, value, () => OnPropertyChanged(nameof(HasSelection)));
    }

    /// <summary>
    /// Gets whether a package is picked (OK is enabled then).
    /// </summary>
    public bool HasSelection => selectedPackage is not null;

    /// <summary>
    /// Gets the picked packages, as <see cref="Services.IEditorDialogService.ShowPackagePickerDialogAsync"/> returns them.
    /// </summary>
    public IReadOnlyCollection<PickablePackageViewModel> SelectedPackages => selectedPackage is null ? [] : [selectedPackage];
}
