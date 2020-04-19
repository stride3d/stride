// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor.Services;
using Stride.Assets.Presentation.ViewModel.Commands;
using Stride.Engine;

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
        /// <param name="session">The current session.</param>
        public PickupEntityCommand(SessionViewModel session)
            : base(session)
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
            var pickerDialog = Session.ServiceProvider.Get<IStrideDialogService>().CreateEntityPickerDialog((EntityHierarchyEditorViewModel)asset.Editor);
            pickerDialog.Filter = item => item is EntityHierarchyRootViewModel || item.Asset == asset;
            return pickerDialog;
        }
    }
}
