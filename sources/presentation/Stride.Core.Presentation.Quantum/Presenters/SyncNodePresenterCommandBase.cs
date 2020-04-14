// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    /// <summary>
    /// Base class for node commands that are not asynchronous.
    /// </summary>
    public abstract class SyncNodePresenterCommandBase : NodePresenterCommandBase
    {
        /// <inheritdoc/>
        [NotNull]
        public sealed override Task Execute(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            ExecuteSync(nodePresenter, parameter, preExecuteResult);
            return Task.CompletedTask;
        }

        protected abstract void ExecuteSync([NotNull] INodePresenter nodePresenter, object parameter, object preExecuteResult);
    }
}
