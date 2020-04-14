// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Quantum.Presenters
{
    public class AnonymousNodePresenterCommand : NodePresenterCommandBase
    {
        private readonly Func<INodePresenter, object, Task> execute;
        private readonly Func<INodePresenter, bool> canAttach;

        public AnonymousNodePresenterCommand([NotNull] string name, [NotNull] Func<INodePresenter, object, Task> execute, [CanBeNull] Func<INodePresenter, bool> canAttach = null)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            this.execute = execute;
            this.canAttach = canAttach;
            Name = name;
        }

        /// <inheritdoc/>
        public override string Name { get; }

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return canAttach?.Invoke(nodePresenter) ?? true;
        }

        /// <inheritdoc/>
        public override Task Execute(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            return execute(nodePresenter, parameter);
        }
    }
}
