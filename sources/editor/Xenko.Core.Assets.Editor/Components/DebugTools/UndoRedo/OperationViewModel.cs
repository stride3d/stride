// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xenko.Core.Transactions;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.DebugTools.UndoRedo
{
    public class OperationViewModel : DispatcherViewModel
    {
        protected readonly IUndoRedoService UndoRedo;

        public OperationViewModel(IViewModelServiceProvider serviceProvider, IUndoRedoService undoRedo, Operation operation)
            : base(serviceProvider)
        {
            UndoRedo = undoRedo;
            Operation = operation;
            var transaction = operation as IReadOnlyTransaction;
            if (transaction != null)
            {
                Children.AddRange(transaction.Operations.Select(x => new OperationViewModel(ServiceProvider, UndoRedo, x)));
            }
        }

        public string Name => UndoRedo.GetName(Operation);

        public string Type => Operation.GetType().Name;

        public ObservableList<OperationViewModel> Children { get; } = new ObservableList<OperationViewModel>();

        internal Operation Operation { get; }
    }
}
