// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Assets.Templates;
using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The parameters asked for a dotnet new template before it is instantiated: one row per user-facing parameter of the template,
/// and the asset packs to add to the game when the template offers them. The toolkit shows it in a dialog and reads
/// <see cref="GetParameterValues"/> and <see cref="SelectedAssetPackIdentities"/> when the user accepts.
/// </summary>
/// <remarks>
/// <para>The hidden parameters are the name (collected by the new project window), the symbols that are not parameters
/// (computed, generated and bound symbols), the ones the template's host file hides, and the single-choice ones (tags).</para>
/// <para>HDR only makes sense from graphics profile 10.0 (shader model 4): the HDR check box is disabled and cleared while 9.0 is
/// selected, and restored to the user's last choice when a higher profile is selected again.</para>
/// </remarks>
public sealed class DotNetNewTemplateParametersViewModel : DispatcherViewModel
{
    private const string GraphicsProfileParameterName = "graphicsProfile";
    private const string HdrParameterName = "HDR";
    private const string LowestGraphicsProfile = "9.0";

    private readonly ChoiceTemplateParameterViewModel? graphicsProfile;
    private readonly BoolTemplateParameterViewModel? hdr;
    private bool? hdrLastUserChoice;
    private bool isLoadingAssetPacks;
    private string? assetPacksStatus;
    private double assetPacksDownloadProgress;
    private bool isAssetPacksDownloadSizeUnknown = true;
    private bool noAssetPacksAvailable;

    /// <param name="serviceProvider">The services (the dispatcher updates the download status).</param>
    /// <param name="template">The template whose parameters are asked.</param>
    /// <param name="assetPacks">
    /// The asset packs to offer, null for none. While the task runs (the AssetPacks package is downloading), the dialog shows the
    /// download progress; the packs are listed once it completes.
    /// </param>
    public DotNetNewTemplateParametersViewModel(IViewModelServiceProvider serviceProvider, ITemplateInfo template, Task<IReadOnlyList<ITemplateInfo>>? assetPacks = null)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
        TemplateName = template.Name ?? template.Identity;
        TemplateDescription = template.Description;
        Title = $"New project — {TemplateName}";

        var parameters = new List<TemplateParameterViewModel>();
        foreach (var parameter in GetVisibleParameters(template))
        {
            var viewModel = CreateParameterViewModel(parameter);
            parameters.Add(viewModel);
            if (viewModel is ChoiceTemplateParameterViewModel choice && string.Equals(parameter.Name, GraphicsProfileParameterName, StringComparison.Ordinal))
                graphicsProfile = choice;
            else if (viewModel is BoolTemplateParameterViewModel check && string.Equals(parameter.Name, HdrParameterName, StringComparison.Ordinal))
                hdr = check;
        }
        Parameters = parameters;

        if (graphicsProfile is not null && hdr is not null)
        {
            hdrLastUserChoice = hdr.IsChecked;
            graphicsProfile.PropertyChanged += OnGraphicsProfileChanged;
            hdr.PropertyChanged += OnHdrChanged;
            SyncHdrEnabled();
        }

        OffersAssetPacks = assetPacks is not null;
        if (assetPacks is not null)
            LoadAssetPacks(assetPacks).Forget();
    }

    /// <summary>
    /// The template whose parameters are asked.
    /// </summary>
    public ITemplateInfo Template { get; }

    public string Title { get; }

    public string TemplateName { get; }

    public string? TemplateDescription { get; }

    public bool HasTemplateDescription => !string.IsNullOrEmpty(TemplateDescription);

    /// <summary>
    /// The user-facing parameters, in the template's declaration order.
    /// </summary>
    public IReadOnlyList<TemplateParameterViewModel> Parameters { get; }

    /// <summary>
    /// Whether the template offers asset packs (the asset packs row is shown).
    /// </summary>
    public bool OffersAssetPacks { get; }

    /// <summary>
    /// The asset packs offered, once loaded.
    /// </summary>
    public ObservableList<AssetPackViewModel> AssetPacks { get; } = [];

    /// <summary>
    /// Whether the asset packs are still being loaded (the AssetPacks package is downloading).
    /// </summary>
    public bool IsLoadingAssetPacks
    {
        get => isLoadingAssetPacks;
        private set => SetValue(ref isLoadingAssetPacks, value);
    }

    /// <summary>
    /// The download status shown while the asset packs load.
    /// </summary>
    public string? AssetPacksStatus
    {
        get => assetPacksStatus;
        private set => SetValue(ref assetPacksStatus, value);
    }

    /// <summary>
    /// The progress of the download, from 0 to 1, when its size is known.
    /// </summary>
    public double AssetPacksDownloadProgress
    {
        get => assetPacksDownloadProgress;
        private set => SetValue(ref assetPacksDownloadProgress, value);
    }

    /// <summary>
    /// Whether the size of the download is unknown (the progress bar is indeterminate).
    /// </summary>
    public bool IsAssetPacksDownloadSizeUnknown
    {
        get => isAssetPacksDownloadSizeUnknown;
        private set => SetValue(ref isAssetPacksDownloadSizeUnknown, value);
    }

    /// <summary>
    /// Whether no asset pack could be offered (the AssetPacks package could not be downloaded).
    /// </summary>
    public bool NoAssetPacksAvailable
    {
        get => noAssetPacksAvailable;
        private set => SetValue(ref noAssetPacksAvailable, value);
    }

    /// <summary>
    /// The identities of the checked asset packs.
    /// </summary>
    public IReadOnlyList<string> SelectedAssetPackIdentities => AssetPacks.Where(x => x.IsChecked).Select(x => x.Identity).ToList();

    /// <summary>
    /// Gets the parameter values to pass to the template engine: only the values that differ from the template's defaults, which the
    /// engine applies itself.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetParameterValues()
    {
        var values = new Dictionary<string, string>();
        foreach (var parameter in Parameters)
        {
            var value = parameter.GetValue();
            if (value is not null && value != parameter.DefaultValue)
                values[parameter.Name] = value;
        }
        return values;
    }

    /// <summary>
    /// Gets the prompt result the generator expects, from the current values.
    /// </summary>
    public DotNetNewPromptResult GetResult() => new(GetParameterValues(), SelectedAssetPackIdentities);

    /// <summary>
    /// The parameters of the template that the dialog shows, in declaration order.
    /// </summary>
    public static IReadOnlyList<ITemplateParameter> GetVisibleParameters(ITemplateInfo template)
    {
        ArgumentNullException.ThrowIfNull(template);
        // In declaration order, which ParameterDefinitions preserves. The host file (dotnetcli.host.json) hides some parameters
        // (e.g. updateOnly).
        var hidden = DotNetNewTemplateBridge.GetHiddenParameterNames(template);
        return template.ParameterDefinitions
            // The built-in "name" parameter comes from sourceName (its IsName flag is not reliably set, so the name is checked too);
            // the new project window collects the project name.
            .Where(p => string.Equals(p.Type, "parameter", StringComparison.Ordinal)
                        && !p.IsName
                        && !string.Equals(p.Name, "name", StringComparison.Ordinal))
            .Where(p => !hidden.Contains(p.Name))
            // A single-choice parameter is a tag (language, type), not a choice
            .Where(p => !(string.Equals(p.DataType, "choice", StringComparison.OrdinalIgnoreCase) && p.Choices is { Count: <= 1 }))
            .ToList();
    }

    /// <summary>
    /// Creates the view model matching the type of a parameter.
    /// </summary>
    public static TemplateParameterViewModel CreateParameterViewModel(ITemplateParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var dataType = (parameter.DataType ?? string.Empty).ToLowerInvariant();
        if (dataType == "bool")
            return new BoolTemplateParameterViewModel(parameter);
        if (dataType == "choice" && parameter.Choices is { Count: > 0 })
        {
            return parameter.AllowMultipleValues
                ? new MultiChoiceTemplateParameterViewModel(parameter)
                : new ChoiceTemplateParameterViewModel(parameter);
        }
        return new TextTemplateParameterViewModel(parameter);
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        if (graphicsProfile is not null)
            graphicsProfile.PropertyChanged -= OnGraphicsProfileChanged;
        if (hdr is not null)
            hdr.PropertyChanged -= OnHdrChanged;
        TemplateDownloads.Changed -= OnDownloadChanged;
        base.Destroy();
    }

    private void OnGraphicsProfileChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChoiceTemplateParameterViewModel.SelectedChoice))
            SyncHdrEnabled();
    }

    private void OnHdrChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only the user's choices are remembered, not the clearing when the profile goes down to 9.0
        if (e.PropertyName == nameof(BoolTemplateParameterViewModel.IsChecked) && hdr!.IsEnabled)
            hdrLastUserChoice = hdr.IsChecked;
    }

    private void SyncHdrEnabled()
    {
        if (graphicsProfile is null || hdr is null)
            return;
        var hdrAllowed = !string.Equals(graphicsProfile.SelectedChoice, LowestGraphicsProfile, StringComparison.Ordinal);
        if (!hdrAllowed)
        {
            hdr.IsEnabled = false;
            hdr.IsChecked = false;
        }
        else
        {
            hdr.IsEnabled = true;
            hdr.IsChecked = hdrLastUserChoice ?? false;
        }
    }

    private async Task LoadAssetPacks(Task<IReadOnlyList<ITemplateInfo>> assetPacks)
    {
        if (!assetPacks.IsCompleted)
        {
            IsLoadingAssetPacks = true;
            UpdateDownloadStatus();
            TemplateDownloads.Changed += OnDownloadChanged;
        }

        IReadOnlyList<ITemplateInfo> packs;
        try
        {
            packs = await assetPacks;
        }
        catch (OperationCanceledException)
        {
            // Same as no pack offered; other failures are reported as an empty list by GetAssetPackTemplatesAsync
            packs = [];
        }
        finally
        {
            TemplateDownloads.Changed -= OnDownloadChanged;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            IsLoadingAssetPacks = false;
            AssetPacksStatus = null;
            if (packs.Count == 0)
            {
                NoAssetPacksAvailable = true;
                return;
            }
            AssetPacks.AddRange(packs.Select(x => new AssetPackViewModel(x)));
        });
    }

    private void OnDownloadChanged()
    {
        Dispatcher.InvokeAsync(UpdateDownloadStatus).Forget();
    }

    private void UpdateDownloadStatus()
    {
        var download = TemplateDownloads.Current;
        AssetPacksStatus = download is null ? "Preparing the asset packs..." : TemplateDownloads.Describe(download);
        IsAssetPacksDownloadSizeUnknown = download?.Fraction is null;
        AssetPacksDownloadProgress = download?.Fraction ?? 0;
    }
}
