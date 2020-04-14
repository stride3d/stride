// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Quantum.ViewModels
{
    /// <summary>
    /// An implementation of <see cref="NodePresenterCommandWrapper"/> that creates transaction on the <see cref="IUndoRedoService"/> if available.
    /// </summary>
    public class AssetNodePresenterCommandWrapper : NodePresenterCommandWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetNodePresenterCommandWrapper"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider of the view model.</param>
        /// <param name="nodePresenters">The <see cref="INodePresenter"/> instances on which to invoke the command.</param>
        /// <param name="command">The command to invoke.</param>
        public AssetNodePresenterCommandWrapper([NotNull] IViewModelServiceProvider serviceProvider, IReadOnlyCollection<INodePresenter> nodePresenters, INodePresenterCommand command)
            : base(serviceProvider, nodePresenters, command)
        {
        }

        /// <inheritdoc/>
        public override async Task Invoke(object parameter)
        {
            var undoRedoService = ServiceProvider.TryGet<IUndoRedoService>();
            using (var transaction = undoRedoService?.CreateTransaction())
            {
                await base.Invoke(parameter);

                if (transaction != null)
                {
                    undoRedoService.SetName(transaction, ActionName);
                }
            }
        }
    }
}
