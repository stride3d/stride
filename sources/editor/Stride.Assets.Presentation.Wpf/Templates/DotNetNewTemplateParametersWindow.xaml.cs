// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TextBox = System.Windows.Controls.TextBox;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Core.Presentation.Controls;
using Stride.Data;
using SDDialogResult = Stride.Core.Presentation.Services.DialogResult;

namespace Stride.Assets.Presentation.Templates;

/// <summary>
/// Dynamic dialog that renders one input control per user-facing <see cref="ITemplateParameter"/>
/// of a dotnet new template (bool → CheckBox, choice → ComboBox, multichoice → CheckBox list,
/// string → TextBox), then returns the user's selections as the parameter dictionary
/// <see cref="DotNetNewTemplateRegistry.InstantiateAsync"/> expects.
/// </summary>
/// <remarks>
/// Skips <c>IsName</c> (the dotnet new <c>-n</c> sourceName; collected separately by the
/// generator's name prompt), and any non-"parameter" symbol type — computed/generated/bind
/// symbols are derived, not user input.
/// </remarks>
public partial class DotNetNewTemplateParametersWindow : ModalWindow
{
    /// <summary>Per-parameter UI binding callback: returns the chosen value as a string (or null for "use default").</summary>
    private readonly List<(ITemplateParameter Param, Func<string?> Read)> bindings = new();

    /// <summary>
    /// Built-in cross-parameter coupling: HDR is meaningful only on graphics feature level 10.0+
    /// (the sample's HDR materials use shader features not available below SM4). Holds the
    /// graphicsProfile combo and the HDR checkbox so a SelectionChanged handler can disable HDR
    /// when the user picks 9.0 (and re-enable + restore prior state when they go back up).
    /// </summary>
    private ComboBox? graphicsProfileCombo;
    private CheckBox? hdrCheckBox;
    private bool? hdrLastUserChoice;

    public string Title { get; }
    public string TemplateName { get; }
    public string? TemplateDescription { get; }

    /// <summary>
    /// Parameter values collected from the dialog. Populated on OK; only entries the user actually
    /// touched (or for which a non-default value is meaningful) are included — TemplateEngine
    /// applies defaults for everything else.
    /// </summary>
    public Dictionary<string, string> Parameters { get; } = new();

    public DotNetNewTemplateParametersWindow(ITemplateInfo template)
    {
        ArgumentNullException.ThrowIfNull(template);
        TemplateName = template.Name ?? template.Identity;
        TemplateDescription = template.Description;
        Title = $"New project — {TemplateName}";

        InitializeComponent();
        DataContext = this;
        if (string.IsNullOrEmpty(TemplateDescription))
            DescriptionTextBlock.Visibility = Visibility.Collapsed;

        BuildControls(template);
    }

    private void BuildControls(ITemplateInfo template)
    {
        // Parameters surface in template.json declaration order (ITemplateInfo.ParameterDefinitions
        // preserves it). Don't sort by Precedence — it's a non-IComparable struct that breaks LINQ
        // OrderBy with 2+ params.
        var visibleParams = template.ParameterDefinitions
            // Exclude the built-in "name" parameter (auto-injected from sourceName; its IsName
            // flag isn't reliably set, so match by name too). GameStudio's outer New-Project
            // flow already collects the project name and passes it via parameters.Name.
            .Where(p => string.Equals(p.Type, "parameter", StringComparison.Ordinal)
                        && !p.IsName
                        && !string.Equals(p.Name, "name", StringComparison.Ordinal))
            // Hide single-choice parameters (e.g. template.json "tags" like language/type).
            .Where(p => !(string.Equals(p.DataType, "choice", StringComparison.OrdinalIgnoreCase)
                          && p.Choices != null && p.Choices.Count <= 1))
            .ToList();

        foreach (var p in visibleParams)
            ParametersPanel.Children.Add(BuildOneRow(p));

        // Wire HDR/graphicsProfile coupling once both rows exist. Initial pass syncs the HDR
        // checkbox's enabled state to the default profile; SelectionChanged keeps it in sync.
        if (graphicsProfileCombo != null && hdrCheckBox != null)
        {
            hdrLastUserChoice = hdrCheckBox.IsChecked;
            graphicsProfileCombo.SelectionChanged += (_, _) => SyncHdrEnabled();
            hdrCheckBox.Checked   += (_, _) => { if (hdrCheckBox.IsEnabled) hdrLastUserChoice = true; };
            hdrCheckBox.Unchecked += (_, _) => { if (hdrCheckBox.IsEnabled) hdrLastUserChoice = false; };
            SyncHdrEnabled();
        }
    }

    /// <summary>
    /// HDR requires graphicsProfile >= 10.0 (Shader Model 4). When the user picks 9.0, force the
    /// HDR checkbox off and disable it; when they pick 10.0+, re-enable and restore the user's
    /// last touched state (so toggling profile back and forth doesn't lose their HDR pref).
    /// </summary>
    private void SyncHdrEnabled()
    {
        if (graphicsProfileCombo == null || hdrCheckBox == null)
            return;
        var profile = graphicsProfileCombo.SelectedItem as string;
        var hdrAllowed = !string.Equals(profile, "9.0", StringComparison.Ordinal);
        hdrCheckBox.IsEnabled = hdrAllowed;
        hdrCheckBox.IsChecked = hdrAllowed && (hdrLastUserChoice ?? false);
    }

    private UIElement BuildOneRow(ITemplateParameter p)
    {
        var row = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };
        var displayName = Humanize(p.Name);
        row.Children.Add(new TextBlock
        {
            Text = string.IsNullOrEmpty(p.Description) ? displayName : $"{displayName} — {p.Description}",
            Margin = new Thickness(0, 0, 0, 3),
            TextWrapping = TextWrapping.Wrap,
        });

        var dataType = (p.DataType ?? string.Empty).ToLowerInvariant();
        if (dataType == "bool")
        {
            var cb = new CheckBox { Content = "Enabled", IsChecked = ParseBool(p.DefaultValue) };
            row.Children.Add(cb);
            bindings.Add((p, () => (cb.IsChecked == true).ToString().ToLowerInvariant()));
            if (string.Equals(p.Name, "HDR", StringComparison.Ordinal))
                hdrCheckBox = cb;
        }
        else if (dataType == "choice" && p.Choices != null && p.Choices.Count > 0)
        {
            if (p.AllowMultipleValues)
            {
                // Multi-choice: render as a vertical list of checkboxes, one per choice. The
                // dotnet new wire format joins selected values with '|' (e.g. "windows|linux").
                // "host" is a CLI convenience (auto-detect host OS); inside GameStudio that
                // sentinel is meaningless — we already know the host — so hide it from the UI
                // and substitute the current OS name into the defaults so something is pre-
                // checked. The platforms-parameter computed bools still recognize "host" if a
                // user passes it explicitly via the CLI path.
                var hideHost = string.Equals(p.Name, "platforms", StringComparison.Ordinal);
                var checks = new List<(string Choice, CheckBox CheckBox)>();
                var defaults = new HashSet<string>((p.DefaultValue ?? string.Empty)
                    .Split('|', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
                if (hideHost && defaults.Remove("host"))
                    defaults.Add(CurrentHostPlatformChoice());
                foreach (var c in p.Choices)
                {
                    if (hideHost && string.Equals(c.Key, "host", StringComparison.Ordinal))
                        continue;
                    var item = new CheckBox
                    {
                        Content = BuildChoiceContent(p.Name, c.Key, c.Value.Description),
                        IsChecked = defaults.Contains(c.Key),
                        Margin = new Thickness(0, 2, 0, 2),
                    };
                    row.Children.Add(item);
                    checks.Add((c.Key, item));
                }
                bindings.Add((p, () =>
                {
                    var selected = checks.Where(t => t.CheckBox.IsChecked == true).Select(t => t.Choice).ToList();
                    return selected.Count == 0 ? null : string.Join("|", selected);
                }));
            }
            else
            {
                // Single choice: ComboBox. Use the choice keys as items; show description as tooltip.
                var combo = new ComboBox { ItemsSource = p.Choices.Keys.ToList() };
                combo.SelectedItem = p.Choices.ContainsKey(p.DefaultValue ?? string.Empty) ? p.DefaultValue : p.Choices.Keys.FirstOrDefault();
                row.Children.Add(combo);
                bindings.Add((p, () => combo.SelectedItem as string));
                if (string.Equals(p.Name, "graphicsProfile", StringComparison.Ordinal))
                    graphicsProfileCombo = combo;
            }
        }
        else
        {
            // string / int / float / unknown: free-form TextBox. TemplateEngine validates on
            // CreateAsync; for now we trust user input.
            var tb = new TextBox { Text = p.DefaultValue ?? string.Empty };
            row.Children.Add(tb);
            bindings.Add((p, () => string.IsNullOrEmpty(tb.Text) ? null : tb.Text));
        }
        return row;
    }

    /// <summary>
    /// Builds a CheckBox.Content payload for a multi-choice row. For the 'platforms' parameter,
    /// returns a horizontal stack with the OS icon (from the shared ImageDictionary, keyed by
    /// <see cref="ConfigPlatforms"/> enum value) + the descriptive label. For everything else,
    /// just the label string — WPF unboxes it as text.
    /// </summary>
    private object BuildChoiceContent(string paramName, string choiceKey, string? description)
    {
        var label = string.IsNullOrEmpty(description) ? Humanize(choiceKey) : description;
        if (!string.Equals(paramName, "platforms", StringComparison.Ordinal))
            return label;
        // dotnet new's lowercase choice keys vs ConfigPlatforms PascalCase enum names — explicit
        // map both because Enum.TryParse(ignoreCase) can't reconcile "macos" → "macOS".
        var platform = choiceKey switch
        {
            "windows" => ConfigPlatforms.Windows,
            "linux"   => ConfigPlatforms.Linux,
            "macos"   => ConfigPlatforms.macOS,
            "ios"     => ConfigPlatforms.iOS,
            "android" => ConfigPlatforms.Android,
            _         => ConfigPlatforms.None,
        };
        if (platform == ConfigPlatforms.None)
            return label;
        // SetResourceReference resolves lazily against the full merged-dictionary chain (window
        // → app → themes), so it picks up the platform DrawingImage from ImageDictionary.xaml
        // regardless of which scope owns it. Falls back gracefully (no image) if missing.
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        var img = new Image { Width = 16, Height = 16, Margin = new Thickness(0, 0, 6, 0) };
        img.SetResourceReference(Image.SourceProperty, platform);
        stack.Children.Add(img);
        stack.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        return stack;
    }

    private static bool ParseBool(string? s) => bool.TryParse(s, out var b) && b;

    /// <summary>Param-name → user-friendly label: splits camelCase/kebab/snake, capitalizes words, preserves ALLCAPS acronyms.</summary>
    internal static string Humanize(string? name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '-' || c == '_') { if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' '); continue; }
            // Insert space before an uppercase letter that starts a new word (lower→Upper, or Upper followed by lower in a run of caps).
            if (i > 0 && char.IsUpper(c) && sb.Length > 0 && sb[sb.Length - 1] != ' ')
            {
                var prev = name[i - 1];
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                if (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && char.IsLower(next)))
                    sb.Append(' ');
            }
            sb.Append(sb.Length == 0 || sb[sb.Length - 1] == ' ' ? char.ToUpperInvariant(c) : c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Maps the OS the editor is running on to the matching <c>platforms</c> choice value
    /// (windows / linux / macos). Used to substitute for "host" when the dialog hides it.
    /// </summary>
    private static string CurrentHostPlatformChoice()
    {
        if (System.OperatingSystem.IsWindows()) return "windows";
        if (System.OperatingSystem.IsLinux())   return "linux";
        if (System.OperatingSystem.IsMacOS())   return "macos";
        return "windows"; // last-resort fallback for GameStudio, which today is Windows-only
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        Parameters.Clear();
        foreach (var (param, read) in bindings)
        {
            var value = read();
            // Only emit values that differ from the default — TemplateEngine fills defaults
            // automatically, and forwarding the default redundantly can confuse downstream
            // computed-bool evaluators (e.g. when a user-passed "Host" overrides nothing).
            if (value != null && value != param.DefaultValue)
                Parameters[param.Name] = value;
        }
        RequestClose(SDDialogResult.Ok);
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        RequestClose(SDDialogResult.Cancel);
    }
}
