// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel<GameSettingsAsset>]
    public class GameSettingsViewModel : AssetViewModel<GameSettingsAsset>
    {
        public const string AvailableFilters = "AvailableFilters";

        private RequiredDisplayOrientation displayOrientation;

        public GameSettingsViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
            displayOrientation = Asset.GetOrCreate<RenderingSettings>().DisplayOrientation;
        }

        protected override void OnSessionSaved()
        {
            base.OnSessionSaved();

            //display orientation needs changes in ios / android manifest files
            // Asset rather than the instance seen at construction: it can be replaced, by a reload of its file for instance
            var currentOrientation = Asset.GetOrCreate<RenderingSettings>().DisplayOrientation;
            if (displayOrientation != currentOrientation && Session.CurrentProject != null)
            {
                GameSettingsAssetCompiler.SetPlatformOrientation(Session.CurrentProject.Project, currentOrientation);

                displayOrientation = currentOrientation;
            }
        }

        public override bool IsLocked => false;

        public SceneViewModel DefaultScene
        {
            get
            {
                if (Asset.DefaultScene == null)
                    return null;

                var reference = AttachedReferenceManager.GetAttachedReference(Asset.DefaultScene);
                return (SceneViewModel)Session.GetAssetById(reference.Id);
            }
            set { SetValue(DefaultScene != value, () => UpdateGameSettings(value)); }
        }

        private void UpdateGameSettings(SceneViewModel scene)
        {
            Asset.DefaultScene = ContentReferenceHelper.CreateReference<Scene>(scene);
        }
    }
}
