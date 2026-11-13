// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;
using Stride.Core;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// A multiple-choice parameter, shown as a list of check boxes. The value joins the checked choices with '|', as dotnet new expects
/// (e.g. "windows|linux").
/// </summary>
/// <remarks>
/// For the "platforms" parameter, the "host" choice (the CLI's "the OS this runs on") is hidden and replaced in the default value by
/// the current OS, which the editor knows.
/// </remarks>
public sealed class MultiChoiceTemplateParameterViewModel : TemplateParameterViewModel
{
    public const string PlatformsParameterName = "platforms";
    private const string HostChoice = "host";

    public MultiChoiceTemplateParameterViewModel(ITemplateParameter parameter)
        : base(parameter)
    {
        var isPlatforms = string.Equals(parameter.Name, PlatformsParameterName, StringComparison.Ordinal);
        var defaults = new HashSet<string>((parameter.DefaultValue ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        if (isPlatforms && defaults.Remove(HostChoice))
            defaults.Add(CurrentHostPlatformChoice());

        var choices = new List<TemplateChoiceViewModel>();
        foreach (var (key, choice) in parameter.Choices ?? new Dictionary<string, ParameterChoice>())
        {
            if (isPlatforms && string.Equals(key, HostChoice, StringComparison.Ordinal))
                continue;
            var label = string.IsNullOrEmpty(choice.Description) ? Humanize(key) : choice.Description;
            choices.Add(new TemplateChoiceViewModel(key, label, defaults.Contains(key), isPlatforms ? ToPlatform(key) : null));
        }
        Choices = choices;
    }

    /// <summary>
    /// The choices offered, in the template's order.
    /// </summary>
    public IReadOnlyList<TemplateChoiceViewModel> Choices { get; }

    /// <inheritdoc/>
    public override string? GetValue()
    {
        var selected = Choices.Where(x => x.IsChecked).Select(x => x.Key).ToList();
        return selected.Count == 0 ? null : string.Join("|", selected);
    }

    /// <summary>
    /// Maps the OS the editor runs on to the matching "platforms" choice (windows, linux or macos).
    /// </summary>
    public static string CurrentHostPlatformChoice()
    {
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "macos";
        return "windows";
    }

    /// <summary>
    /// Maps a "platforms" choice to its <see cref="PlatformType"/>; null for a choice without a platform (or an unknown one).
    /// </summary>
    public static PlatformType? ToPlatform(string choice)
    {
        // The choice keys are lower case, the enum names are not (macOS, iOS): an explicit map
        return choice switch
        {
            "windows" => PlatformType.Windows,
            "linux" => PlatformType.Linux,
            "macos" => PlatformType.macOS,
            "ios" => PlatformType.iOS,
            "android" => PlatformType.Android,
            _ => null,
        };
    }
}
