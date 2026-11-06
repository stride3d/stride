// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Scripts;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using CoreEditorSettings = Stride.Core.Assets.Editor.Settings.EditorSettings;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Creates a script source file from the <c>.cs</c> next to the template, after asking its class name and namespace (see
/// <see cref="IScriptNamePrompt"/>) and whether to save the session (a script is compiled once saved).
/// </summary>
public sealed class ScriptTemplateGenerator : AssetTemplateGenerator
{
    private const string AssetTypeName = "ScriptSourceFileAsset";

    private static readonly PropertyKey<string?> ClassNameKey = new("ClassNameKey", typeof(ScriptTemplateGenerator));

    private static readonly PropertyKey<string?> DefaultClassNameKey = new(nameof(DefaultClassNameKey), typeof(ScriptTemplateGenerator));

    private static readonly PropertyKey<bool> EnableTemplateSelectKey = new(nameof(EnableTemplateSelectKey), typeof(ScriptTemplateGenerator));

    private static readonly PropertyKey<bool> SaveSessionKey = new("SaveSessionKey", typeof(ScriptTemplateGenerator));

    private readonly IScriptNamePrompt prompt;

    /// <param name="prompt">The dialog asking the names of the script.</param>
    public ScriptTemplateGenerator(IScriptNamePrompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        this.prompt = prompt;
    }

    public static void SetClassName(AssetTemplateGeneratorParameters parameters, string className) => parameters.Tags.Set(ClassNameKey, className);

    /// <summary>
    /// The script templates among the given ones.
    /// </summary>
    public static IEnumerable<TemplateAssetDescription> GetScriptTemplateAssetDescriptions(IEnumerable<TemplateDescription> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        return templates.OfType<TemplateAssetDescription>().Where(t => t.AssetTypeName == AssetTypeName);
    }

    /// <summary>
    /// The parameters of a script created for a component: its proposed class name, and whether the user can pick another
    /// script template.
    /// </summary>
    public static PropertyContainer GetAssetOverrideParameters(string? defaultClassName, bool enableTemplateSelect = true)
    {
        var customParameters = new PropertyContainer();
        if (!string.IsNullOrEmpty(defaultClassName))
        {
            customParameters[DefaultClassNameKey] = defaultClassName;
        }
        customParameters[EnableTemplateSelectKey] = enableTemplateSelect;
        return customParameters;
    }

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        if (templateDescription is not TemplateAssetDescription assetTemplate)
            return false;

        var assetType = assetTemplate.FindAssetType();
        return assetType is not null && typeof(ScriptSourceFileAsset).IsAssignableFrom(assetType);
    }

    /// <inheritdoc/>
    protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        if (parameters.Unattended)
        {
            // TODO: Some templates save, some don't (and behavior can even change in Unattended mode)
            // For consistency, should this be moved to a common parameters.Saving?
            parameters.Tags.Set(SaveSessionKey, false);
            return true;
        }

        var defaultClassName = parameters.Description.DefaultOutputName ?? parameters.Name;
        if (parameters.Tags.TryGetValue(DefaultClassNameKey, out var className) && !string.IsNullOrEmpty(className))
        {
            defaultClassName = className;
        }

        // Suffix the proposed name if a script with the same name already exists in the package
        // (the Add Asset dialog dedups the default name the same way).
        defaultClassName = NamingHelper.ComputeNewName(defaultClassName,
            name => parameters.Package.Assets.Any(a => a.Asset is ScriptSourceFileAsset
                && string.Equals(a.Location.GetFileNameWithoutExtension(), name, StringComparison.OrdinalIgnoreCase)),
            "{0}{1}");

        var enableTemplateSelect = parameters.TryGetTag(EnableTemplateSelectKey);
        IEnumerable<TemplateAssetDescription>? scriptTemplates = null;
        if (enableTemplateSelect)
        {
            scriptTemplates = GetScriptTemplateAssetDescriptions(TemplateManager.FindTemplates(TemplateScope.Asset, parameters.Package.Session));
        }

        var viewModel = new ScriptNameViewModel(defaultClassName, parameters.Namespace, (TemplateAssetDescription)parameters.Description, enableTemplateSelect, scriptTemplates);
        if (!await prompt.PromptAsync(viewModel))
            return false;

        parameters.Namespace = viewModel.Namespace;
        parameters.Tags.Set(ClassNameKey, viewModel.ClassName);
        parameters.Description = viewModel.ScriptTemplate;

        if (CoreEditorSettings.AskBeforeSavingNewScripts.GetValue())
        {
            var buttons = DialogButtons.Create(
            [
                Tr._p("Button", "Save"),
                Tr._p("Button", "Cancel")
            ], 1, 2);
            var message = Tr._p("Message", "You can't use scripts until you save them. Do you want to save now?");
            var result = await Session.ServiceProvider.Get<IDialogService>().CheckedMessageBoxAsync(message, false, CoreEditorSettings.AlwaysSaveNewScriptsWithoutAsking, buttons, MessageBoxImage.Question);

            if (result.IsChecked == true)
            {
                CoreEditorSettings.AskBeforeSavingNewScripts.SetValue(false);
                CoreEditorSettings.Save();
            }
            parameters.Tags.Set(SaveSessionKey, result.Result == 1);
        }
        else
        {
            parameters.Tags.Set(SaveSessionKey, true);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var desc = parameters.Description;
        var scriptFile = Path.ChangeExtension(desc.FullPath, ScriptSourceFileAsset.Extension);

        var scriptContent = File.ReadAllText(scriptFile);
        parameters.Name = parameters.Tags.Get(ClassNameKey) ?? parameters.Name;
        var location = GenerateLocation(parameters);
        scriptContent = scriptContent.Replace("##Namespace##", parameters.Namespace);
        scriptContent = scriptContent.Replace("##Scriptname##", location.GetFileNameWithoutExtension());

        var asset = ObjectFactoryRegistry.NewInstance<ScriptSourceFileAsset>();
        asset.Id = SourceCodeAsset.GenerateIdFromLocation(parameters.Package.Meta.Name!, location);
        asset.Text = scriptContent;
        yield return new AssetItem(location, asset);
    }

    /// <inheritdoc/>
    protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
    {
        parameters.RequestSessionSave = parameters.TryGetTag(SaveSessionKey);
    }
}
