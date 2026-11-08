// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The platforms asked by the Update platforms template (the WPF <c>UpdatePlatformsWindow</c>): one entry per supported platform,
/// checked when the package has it already. Unchecking an installed platform removes its project.
/// </summary>
public sealed class UpdatePlatformsViewModel : ViewModelBase
{
    private bool forcePlatformRegeneration;

    /// <param name="platforms">The platforms offered (<see cref="AssetRegistry.SupportedPlatforms"/>).</param>
    /// <param name="installedPlatforms">The platforms the package has already.</param>
    public UpdatePlatformsViewModel(IEnumerable<SolutionPlatform> platforms, ICollection<PlatformType> installedPlatforms)
    {
        ArgumentNullException.ThrowIfNull(platforms);
        ArgumentNullException.ThrowIfNull(installedPlatforms);
        Platforms = platforms.Select(x => new UpdatePlatformEntryViewModel(x, installedPlatforms.Contains(x.Type))).ToList();
    }

    /// <summary>
    /// Gets the platforms offered.
    /// </summary>
    public IReadOnlyList<UpdatePlatformEntryViewModel> Platforms { get; }

    /// <summary>
    /// Gets or sets whether the projects of the platforms the package has already are generated again.
    /// </summary>
    public bool ForcePlatformRegeneration
    {
        get => forcePlatformRegeneration;
        set => SetValue(ref forcePlatformRegeneration, value);
    }

    /// <summary>
    /// Gets whether a platform is checked: at least one is needed.
    /// </summary>
    public bool HasSelection => Platforms.Any(x => x.IsSelected);

    /// <summary>
    /// Gets how many installed platforms are unchecked, whose projects are removed.
    /// </summary>
    public int RemovedCount => Platforms.Count(x => x.MarkedToRemove);

    /// <summary>
    /// Gets the answer for the generator: the checked platforms and whether to regenerate them.
    /// </summary>
    public UpdatePlatformsResult ToResult() => new(Platforms.Where(x => x.IsSelected).Select(x => x.Platform).ToList(), ForcePlatformRegeneration);
}

/// <summary>
/// A platform of <see cref="UpdatePlatformsViewModel"/>.
/// </summary>
public sealed class UpdatePlatformEntryViewModel : ViewModelBase
{
    private bool isSelected;

    public UpdatePlatformEntryViewModel(SolutionPlatform platform, bool isAlreadyInstalled)
    {
        ArgumentNullException.ThrowIfNull(platform);
        SolutionPlatform = platform;
        IsAlreadyInstalled = isAlreadyInstalled;
        isSelected = isAlreadyInstalled;
        DependentProperties.Add(nameof(IsSelected), [nameof(MarkedToRemove)]);
    }

    public SolutionPlatform SolutionPlatform { get; }

    public string Name => SolutionPlatform.DisplayName ?? SolutionPlatform.Name;

    public PlatformType Platform => SolutionPlatform.Type;

    /// <summary>
    /// Gets whether the platform can be unchecked: Windows can't, as in WPF.
    /// </summary>
    public bool CanBeUnselected => Platform != PlatformType.Windows;

    public bool IsSelected
    {
        get => isSelected;
        set => SetValue(ref isSelected, value);
    }

    /// <summary>
    /// Gets whether the package has the platform already.
    /// </summary>
    public bool IsAlreadyInstalled { get; }

    /// <summary>
    /// Gets whether this machine can build for the platform.
    /// </summary>
    public bool IsAvailableOnMachine => SolutionPlatform.IsAvailable;

    /// <summary>
    /// Gets whether the platform is installed and unchecked: its project will be removed.
    /// </summary>
    public bool MarkedToRemove => !IsSelected && IsAlreadyInstalled;
}
