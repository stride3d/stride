// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Quantum.Presenters
{
    public abstract class NodePresenterCommandBase : INodePresenterCommand
    {
        public abstract string Name { get; }

        public virtual CombineMode CombineMode => CombineMode.CombineOnlyForAll;

        public abstract bool CanAttach(INodePresenter nodePresenter);

        public virtual bool CanExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object parameter)
        {
            return true;
        }

        [NotNull]
        public virtual Task<object> PreExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object parameter)
        {
            return Task.FromResult<object>(null);
        }

        public abstract Task Execute(INodePresenter nodePresenter, object parameter, object preExecuteResult);

        [NotNull]
        public virtual Task PostExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object parameter)
        {
            return Task.CompletedTask;
        }
    }
}
