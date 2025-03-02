// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor.Services;
using Stride.Engine;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Assets.Presentation.NodePresenters.Commands
{
    public class PickupEntityCommand : PickupSceneObjectCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "PickupEntity";

        /// <summary>
        /// Initializes a new instance of the <see cref="PickupEntityCommand"/> class.
        /// </summary>
        public PickupEntityCommand()
        {
        }

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return typeof(Entity).IsAssignableFrom(nodePresenter.Type);
        }

        /// <inheritdoc/>
        protected override object ConvertPickerValue(object newValue, object parameter)
        {
            var pickedEntity = (IPickedEntity)newValue;
            return pickedEntity.Entity?.AssetSideEntity;
        }

        /// <inheritdoc/>
        protected override IEntityPickerDialog CreatePicker(AssetViewModel asset, Type targetType)
        {
            if (asset.ServiceProvider.Get<IAssetEditorsManager>().TryGetAssetEditor<EntityHierarchyEditorViewModel>(asset, out var editor))
            {
                var pickerDialog = asset.ServiceProvider.Get<IStrideDialogService>().CreateEntityPickerDialog(editor);
                pickerDialog.Filter = item => item is EntityHierarchyRootViewModel || item.Asset == asset;
                return pickerDialog;
            }

            return null;
        }
    }
}
