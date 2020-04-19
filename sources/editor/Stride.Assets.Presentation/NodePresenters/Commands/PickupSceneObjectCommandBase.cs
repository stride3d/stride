// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.SceneEditor.Services;

namespace Stride.Assets.Presentation.NodePresenters.Commands
{
    public abstract class PickupSceneObjectCommandBase : ChangeValueWithPickerCommandBase
    {
        /// <summary>
        /// The current session.
        /// </summary>
        protected readonly SessionViewModel Session;

        /// <summary>
        /// Initializes a new instance of the <see cref="PickupSceneObjectCommandBase"/> class.
        /// </summary>
        /// <param name="session">The current session.</param>
        protected PickupSceneObjectCommandBase(SessionViewModel session)
        {
            Session = session;
        }

        /// <inheritdoc/>
        protected override async Task<PickerResult> ShowPicker(IReadOnlyCollection<INodePresenter> nodePresenters, object currentValue, object parameter)
        {
            var parameters = (Tuple<object, object>)parameter;
            var asset = (AssetViewModel)parameters.Item1;
            var targetType = (Type)parameters.Item2;
            var picker = CreatePicker(asset, targetType);
            var result = await picker.ShowModal();

            var pickerResult = new PickerResult
            {
                ProcessChange = result == DialogResult.Ok,
                NewValue = picker.SelectedEntity
            };
            return pickerResult;
        }

        /// <summary>
        /// Creates the adapted picker dialog to select a scene object.
        /// </summary>
        /// <param name="asset">The target asset.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <returns>A new instance of <see cref="IEntityPickerDialog"/>.</returns>
        protected abstract IEntityPickerDialog CreatePicker(AssetViewModel asset, Type targetType);
    }
}
