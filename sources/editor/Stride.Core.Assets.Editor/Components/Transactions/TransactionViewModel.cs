// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Transactions;

namespace Stride.Core.Assets.Editor.Components.Transactions;

public sealed class TransactionViewModel : DispatcherViewModel
{
    private readonly IReadOnlyTransaction transaction;
    private bool isDone = true;
    private bool isSaved;
    private bool isSavePoint;

    public TransactionViewModel(IViewModelServiceProvider serviceProvider, IReadOnlyTransaction transaction)
        : base(serviceProvider)
    {
        this.transaction = transaction;
        Name = ServiceProvider.Get<IUndoRedoService>().GetName(transaction) ?? string.Empty;
    }

    /// <summary>
    /// Name of this transaction.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets whether this transaction is currently done.
    /// </summary>
    public bool IsDone { get { return isDone; } private set { SetValue(ref isDone, value); } }

    /// <summary>
    /// Gets whether this transaction is currently saved.
    /// </summary>
    public bool IsSaved { get { return isSaved; } private set { SetValue(ref isSaved, value); } }

    /// <summary>
    /// Get whether this transaction is the current save point.
    /// </summary>
    public bool IsSavePoint { get { return isSavePoint; } internal set { SetValue(ref isSavePoint, value); } }

    /// <summary>
    /// Gets the unique identifier of this transaction.
    /// </summary>
    public Guid Id => transaction.Id;

    /// <summary>
    /// Refresh the <see cref="IsDone"/> property of this view model.
    /// </summary>
    internal void Refresh()
    {
        var dirtying = transaction.Operations.SelectMany(DirtiableManager.GetDirtyingOperations);
        IsDone = dirtying.All(x => x.IsDone);
    }

    /// <summary>
    /// Notifies that this transaction has been saved and update <see cref="IsSaved"/> accordingly.
    /// </summary>
    internal void NotifySave()
    {
        IsSaved = IsDone;
    }
}
