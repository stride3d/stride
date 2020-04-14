// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.Scripts;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    internal class AddScriptSourceFileAssetPolicy : CreateComponentPolicyBase<ScriptSourceFileAsset, ScriptSourceFileAssetViewModel>
    {
        /// <inheritdoc />
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, ScriptSourceFileAssetViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
        {
            var scriptType = FindScriptType(asset.ServiceProvider, asset.AssetItem)?.FirstOrDefault();
            if (scriptType == null)
            {
                message = $"No scripts inheriting from {nameof(ScriptComponent)} found in asset {asset.Url}";
                return false;
            }

            // TODO: Check how many scripts there is inside this file?
            message = string.Format($"Add script {scriptType.Name} into {{0}}", messageArgs);
            return true;
        }

        /// <inheritdoc />
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, ScriptSourceFileAssetViewModel asset)
        {
            var scriptType = FindScriptType(asset.ServiceProvider, asset.AssetItem)?.FirstOrDefault();
            if (scriptType == null)
                return null;

            try
            {
                return Activator.CreateInstance(scriptType) as EntityComponent;
            }
            catch
            {
                // TODO: Display error message
                return null;
            }
        }

        [CanBeNull]
        private static IEnumerable<Type> FindScriptType([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] AssetItem scriptAsset)
        {
            return serviceProvider.TryGet<IScriptSourceCodeResolver>()?.GetTypesFromSourceFile(scriptAsset.FullPath);
        }
    }
}
