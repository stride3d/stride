// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.SceneEditor.Services;
using Xenko.Assets.Presentation.ViewModel.Commands;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.NodePresenters.Commands
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
            var pickerDialog = Session.ServiceProvider.Get<IXenkoDialogService>().CreateEntityPickerDialog((EntityHierarchyEditorViewModel)asset.Editor);
            pickerDialog.Filter = item => item is EntityHierarchyRootViewModel || item.Asset == asset;
            return pickerDialog;
        }
    }
}
