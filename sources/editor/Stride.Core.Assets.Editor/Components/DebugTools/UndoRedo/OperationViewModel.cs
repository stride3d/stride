// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo
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
