// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;
using Stride.Data;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ViewModel
{
    [AssetViewModel(typeof(GameSettingsAsset))]
    public class GameSettingsViewModel : AssetViewModel<GameSettingsAsset>
    {
        public const string AvailableFilters = "AvailableFilters";

        private readonly GameSettingsAsset gameSettingsAsset;
        private RequiredDisplayOrientation displayOrientation;

        public GameSettingsViewModel(AssetViewModelConstructionParameters parameters)
            : base(parameters)
        {
            gameSettingsAsset = (GameSettingsAsset)AssetItem.Asset;
            displayOrientation = gameSettingsAsset.GetOrCreate<RenderingSettings>().DisplayOrientation;

            var platformFiltersNode = AssetRootNode[nameof(GameSettingsAsset.PlatformFilters)].Target;
            platformFiltersNode.ItemChanging += PlatformFiltersNodeChanging;
            platformFiltersNode.ItemChanged += PlatformFiltersNodeChanged;
        }

        private void PlatformFiltersNodeChanging(object sender, ItemChangeEventArgs e)
        {
            if (e.ChangeType == ContentChangeType.CollectionUpdate)
            {
                var index = e.Index.Int;
                using (var transaction = UndoRedoService.CreateTransaction())
                {
                    foreach (var platformOverride in Asset.Overrides)
                    {
                        var node = Session.AssetNodeContainer.GetNode(platformOverride);
                        var filterNode = node[nameof(ConfigurationOverride.SpecificFilter)];

                        if (platformOverride.SpecificFilter == index)
                        {
                            // This is a hack to force refreshing the display of the filter in override. We clear and reset it before and after the name change.
                            filterNode.Update(-1);
                            filterNode.Update(index);
                        }
                    }
                    UndoRedoService.SetName(transaction, "Force filter refresh");
                }
            }
        }

        private void PlatformFiltersNodeChanged(object sender, ItemChangeEventArgs e)
        {
            var index = e.Index.Int;

            ITransaction transaction = null;
            try
            {
                if (e.ChangeType == ContentChangeType.CollectionUpdate)
                    transaction = UndoRedoService.CreateTransaction();

                foreach (var platformOverride in Asset.Overrides)
                {
                    var node = Session.AssetNodeContainer.GetNode(platformOverride);
                    var filterNode = node[nameof(ConfigurationOverride.SpecificFilter)];

                    switch (e.ChangeType)
                    {
                        case ContentChangeType.CollectionUpdate:
                        {
                            // This is a hack to force refreshing the display of the filter in override. We clear and reset it before and after the name change.
                            if (platformOverride.SpecificFilter == index)
                            {
                                filterNode.Update(-1);
                                filterNode.Update(index);
                            }
                            break;
                        }
                        case ContentChangeType.CollectionAdd:
                        {
                            var filterIndex = (int)filterNode.Retrieve();
                            if (filterIndex >= index)
                            {
                                filterNode.Update(filterIndex + 1);
                            }
                            break;
                        }
                        case ContentChangeType.CollectionRemove:
                        {
                            var filterIndex = (int)filterNode.Retrieve();
                            if (filterIndex > index)
                            {
                                filterNode.Update(filterIndex - 1);
                            }
                            else if (filterIndex == index)
                            {
                                filterNode.Update(-1);
                            }
                            break;
                        }
                    }
                }


            }
            finally
            {
                if (transaction != null)
                {
                    UndoRedoService.SetName(transaction, "Force filter refresh");
                    transaction.Complete();
                }
            }
        }

        protected override void OnSessionSaved()
        {
            base.OnSessionSaved();

            //display orientation needs changes in ios / android manifest files
            var currentOrientation = gameSettingsAsset.GetOrCreate<RenderingSettings>().DisplayOrientation;
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
