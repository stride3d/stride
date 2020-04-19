// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    public class SyncAnonymousNodePresenterCommand : SyncNodePresenterCommandBase
    {
        private readonly Action<INodePresenter, object> execute;
        private readonly Func<INodePresenter, bool> canAttach;

        public SyncAnonymousNodePresenterCommand([NotNull] string name, [NotNull] Action<INodePresenter, object> execute, [CanBeNull] Func<INodePresenter, bool> canAttach = null)
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
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            execute(nodePresenter, parameter);
        }
    }
}
