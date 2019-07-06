// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Resources.Strings;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.Windows;
using Xenko.Core.Translation;
using Xenko.Assets.Scripts;
using EditorViewModel = Xenko.Core.Assets.Editor.ViewModel.EditorViewModel;
using System.Linq;

namespace Xenko.Assets.Presentation.Templates
{
    public class ScriptTemplateGenerator : AssetTemplateGenerator
    {
        private const string AssetTypeName = "ScriptSourceFileAsset";

        public static readonly ScriptTemplateGenerator Default = new ScriptTemplateGenerator();

        private static readonly PropertyKey<string> ClassNameKey = new PropertyKey<string>("ClassNameKey", typeof(ScriptTemplateGenerator));

        private static readonly PropertyKey<string> DefaultClassNameKey = new PropertyKey<string>(nameof(DefaultClassNameKey), typeof(ScriptTemplateGenerator));

        private static readonly PropertyKey<bool> EnableTemplateSelectKey = new PropertyKey<bool>(nameof(EnableTemplateSelectKey), typeof(ScriptTemplateGenerator));

        private static readonly PropertyKey<bool> SaveSessionKey = new PropertyKey<bool>("SaveSessionKey", typeof(ScriptTemplateGenerator));

        public static void SetClassName(AssetTemplateGeneratorParameters parameters, string className) => parameters.Tags.Set(ClassNameKey, className);

        public static IEnumerable<TemplateAssetDescription> GetScriptTemplateAssetDescriptions(IEnumerable<TemplateDescription> templates)
        {
            if (templates == null)
            {
                throw new ArgumentNullException(nameof(templates));
            }

            return templates.OfType<TemplateAssetDescription>().Where(t => t.AssetTypeName == AssetTypeName);
        }

        public static PropertyContainer GetAssetOverrideParameters(string defaultClassName, bool enableTemplateSelect = true)
        {
            var customParameters = new PropertyContainer();

            if (!string.IsNullOrEmpty(defaultClassName))
            {
                customParameters[DefaultClassNameKey] = defaultClassName;
            }

            customParameters[EnableTemplateSelectKey] = enableTemplateSelect;
            return customParameters;
        }

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            var assetTemplate = templateDescription as TemplateAssetDescription;
            if (assetTemplate == null)
                return false;

            var assetType = assetTemplate.GetAssetType();
            return assetType != null && typeof(ScriptSourceFileAsset).IsAssignableFrom(assetType);
        }

        protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
        {
            if (!parameters.Unattended)
            {
                string defaultClassName = parameters.Description.DefaultOutputName;

                if(parameters.Tags.TryGetValue(DefaultClassNameKey, out string className) && !string.IsNullOrEmpty(className))
                {
                    defaultClassName = className;
                }

                bool enableTemplateSelect = parameters.TryGetTag(EnableTemplateSelectKey);

                IEnumerable<TemplateAssetDescription> scriptTemplates = null;

                if (enableTemplateSelect)
                {
                    scriptTemplates = GetScriptTemplateAssetDescriptions(TemplateManager.FindTemplates(TemplateScope.Asset, parameters.Package.Session));
                }

                var window = new ScriptNameWindow(defaultClassName, parameters.Namespace, parameters.Description as TemplateAssetDescription, enableTemplateSelect, scriptTemplates);

                await window.ShowModal();

                if (window.Result == DialogResult.Cancel)
                    return false;

                parameters.Namespace = window.Namespace;
                parameters.Tags.Set(ClassNameKey, window.ClassName);
                parameters.Description = window.ScriptTemplate;

                var ask = Xenko.Core.Assets.Editor.Settings.EditorSettings.AskBeforeSavingNewScripts.GetValue();
                if (ask)
                {
                    var buttons = DialogHelper.CreateButtons(new[]
                    {
                        Tr._p("Button", "Save"),
                        Tr._p("Button", "Cancel")
                    }, 1, 2);
                    var message = Tr._p("Message", "You can't use scripts until you save them. Do you want to save now?");
                    var checkedMessage = Xenko.Core.Assets.Editor.Settings.EditorSettings.AlwaysSaveNewScriptsWithoutAsking;
                    var result = await EditorViewModel.Instance.ServiceProvider.Get<IDialogService>().CheckedMessageBox(message, false, checkedMessage, buttons, MessageBoxImage.Question);

                    if (result.IsChecked == true)
                    {
                        Xenko.Core.Assets.Editor.Settings.EditorSettings.AskBeforeSavingNewScripts.SetValue(false);
                        Xenko.Core.Assets.Editor.Settings.EditorSettings.Save();
                    }
                    parameters.Tags.Set(SaveSessionKey, result.Result == 1);
                }
                else
                {
                    parameters.Tags.Set(SaveSessionKey, true);
                }
            }
            else
            {
                // TODO: Some templates save, some don't (and behavior can even change in Unattended mode)
                // For consistency, should this be moved to a common parameters.Saving?
                parameters.Tags.Set(SaveSessionKey, false);
            }

            return true;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var desc = parameters.Description;
            var scriptFile = Path.ChangeExtension(desc.FullPath, ScriptSourceFileAsset.Extension);

            var scriptContent = File.ReadAllText(scriptFile);
            parameters.Name = parameters.Tags.Get(ClassNameKey);
            var location = GenerateLocation(parameters);
            scriptContent = scriptContent.Replace("##Namespace##", parameters.Namespace);
            scriptContent = scriptContent.Replace("##Scriptname##", location.GetFileNameWithoutExtension());

            var asset = (ScriptSourceFileAsset)ObjectFactoryRegistry.NewInstance(typeof(ScriptSourceFileAsset));
            asset.Id = SourceCodeAsset.GenerateIdFromLocation(parameters.Package.Meta.Name, location);
            asset.Text = scriptContent;
            yield return new AssetItem(location, asset);
        }

        protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
        {
            parameters.RequestSessionSave = parameters.TryGetTag(SaveSessionKey);
        }
    }
}
