// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public abstract class ChangeValueWithPickerCommandBase : ChangeValueCommandBase
    {
        /// <summary>
        /// A structure containing the result of the picker invocation.
        /// </summary>
        protected struct PickerResult
        {
            /// <summary>
            /// Indicates if the picker got validated or cancelled.
            /// </summary>
            public bool ProcessChange;
            /// <summary>
            /// The new value to set.
            /// </summary>
            public object NewValue;
        }

        /// <inheritdoc/>
        public override async Task<object> PreExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object parameter)
        {
            var currentValue = nodePresenters.First().Value;
            var result = await ShowPicker(nodePresenters, currentValue, parameter);
            return result;
        }

        /// <inheritdoc/>
        protected override object ChangeValue(object currentValue, object parameter, object preExecuteResult)
        {
            var pickerResult = (PickerResult)preExecuteResult;
            return pickerResult.ProcessChange ? ConvertPickerValue(pickerResult.NewValue, parameter) : currentValue;
        }

        /// <summary>
        /// Shows the picker related to this command..
        /// </summary>
        /// <param name="nodePresenters"></param>
        /// <param name="currentValue">The current value of the node.</param>
        /// <param name="parameter">The parameter of the command.</param>
        /// <returns>A task that completes when the picker has been closed and returns the result of the picker.</returns>
        protected abstract Task<PickerResult> ShowPicker(IReadOnlyCollection<INodePresenter> nodePresenters, object currentValue, object parameter);

        /// <summary>
        /// Generates the value to assign to the node from the value returned by the picker.
        /// </summary>
        /// <param name="newValue">The value returned by the picker.</param>
        /// <param name="parameter">The parameter of the command.</param>
        /// <returns>The actual value to set.</returns>
        /// <remarks>The default implementation returns the <paramref name="newValue"/> itself.</remarks>
        protected virtual object ConvertPickerValue(object newValue, object parameter) => newValue;
    }
}
