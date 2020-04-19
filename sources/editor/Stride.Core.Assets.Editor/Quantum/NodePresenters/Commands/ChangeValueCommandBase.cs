// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public abstract class ChangeValueCommandBase : SyncNodePresenterCommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var newValue = ChangeValue(nodePresenter.Value, parameter, preExecuteResult);
            nodePresenter.UpdateValue(newValue);
        }

        /// <summary>
        /// Computes the new value to set to the related <see cref="INodePresenter"/> instance.
        /// </summary>
        /// <param name="currentValue">The current value of the node presenter.</param>
        /// <param name="parameter">The parametr of the command.</param>
        /// <param name="preExecuteResult">The result of the <see cref="INodePresenterCommand.PreExecute"/> step.</param>
        /// <returns>The new value to set to the node presenter.</returns>
        protected abstract object ChangeValue(object currentValue, object parameter, object preExecuteResult);
    }
}
