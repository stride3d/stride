// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Quantum.Presenters
{
    public interface INodePresenterCommand
    {
        [NotNull]
        string Name { get; }

        CombineMode CombineMode { get; }

        bool CanAttach([NotNull] INodePresenter nodePresenter);

        bool CanExecute([NotNull] IReadOnlyCollection<INodePresenter> nodePresenters, object parameter);

        Task<object> PreExecute([NotNull] IReadOnlyCollection<INodePresenter> nodePresenters, object parameter);

        Task Execute([NotNull] INodePresenter nodePresenter, object parameter, object preExecuteResult);

        Task PostExecute([NotNull] IReadOnlyCollection<INodePresenter> nodePresenters, object parameter);
    }
}
