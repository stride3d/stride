// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Transactions;

namespace Stride.Core.Assets.Editor.Components.Transactions;

public sealed class ActionHistoryViewModel : DispatcherViewModel
{
    private readonly IUndoRedoService service;
    private readonly SessionViewModel session;
    private readonly ObservableList<TransactionViewModel> transactions = [];

    public ActionHistoryViewModel(SessionViewModel session)
        : base(session.SafeArgument().ServiceProvider)
    {
        this.session = session;
        service = ServiceProvider.Get<IUndoRedoService>();
        transactions.CollectionChanged += (_, _) => RefreshUndoRedoStatus();
        UndoCommand = new AnonymousCommand(ServiceProvider, () => { service.Undo(); /*session.CheckConsistency();*/ RefreshUndoRedoStatus(); });
        RedoCommand = new AnonymousCommand(ServiceProvider, () => { service.Redo(); /*session.CheckConsistency();*/ RefreshUndoRedoStatus(); });
        RefreshUndoRedoStatus();
    }

    /// <summary>
    /// Gets whether it is currently possible to perform an undo operation.
    /// </summary>
    public bool CanUndo => Transactions.Count > 0 && Transactions.First().IsDone;

    /// <summary>
    /// Gets whether it is currently possible to perform a redo operation.
    /// </summary>
    public bool CanRedo => Transactions.Count > 0 && !Transactions.Last().IsDone;

    /// <summary>
    /// Collection of action view models currently contained in this view model.
    /// </summary>
    public IReadOnlyObservableCollection<TransactionViewModel> Transactions => transactions;

    /// <summary>
    /// Command that will perform an undo operation.
    /// </summary>
    public ICommandBase UndoCommand { get; }

    /// <summary>
    /// Command that will perform an redo operation.
    /// </summary>
    public ICommandBase RedoCommand { get; }

    /// <summary>
    /// Initializes this <see cref="ActionHistoryViewModel"/> and starts to listen to <see cref="IUndoRedoService"/> events.
    /// </summary>
    public void Initialize()
    {
        service.Done += TransactionDone;
        service.Undone += TransactionUndoneOrRedone;
        service.Redone += TransactionUndoneOrRedone;
        service.TransactionDiscarded += TransactionDiscarded;
        service.Cleared += Cleared;
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        service.Done -= TransactionDone;
        service.Undone -= TransactionUndoneOrRedone;
        service.Redone -= TransactionUndoneOrRedone;
        service.TransactionDiscarded -= TransactionDiscarded;
        service.Cleared -= Cleared;
        base.Destroy();
    }

    /// <summary>
    /// Notify that everything has been saved and create a save point in the action stack.
    /// </summary>
    internal void NotifySave()
    {
        service.NotifySave();
        transactions.ForEach(x => x.NotifySave());
        transactions.ForEach(x => x.IsSavePoint = false);
        var savePoint = transactions.LastOrDefault(x => x.IsSaved);
        if (savePoint != null)
            savePoint.IsSavePoint = true;
    }
    
    private void Cleared(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => transactions.Clear());
    }
    
    private void TransactionDiscarded(object? sender, TransactionsDiscardedEventArgs e)
    {
        Dispatcher.Invoke(() => transactions.RemoveWhere(x => e.Transactions.Any(y => x.Id == y.Id)));
    }

    private async void TransactionDone(object? sender, TransactionEventArgs e)
    {
        if (e.Transaction.Operations.Count == 0)
            return;

        var dirtying = e.Transaction.Operations.SelectMany(DirtiableManager.GetDirtyingOperations);
        var dirtiables = new HashSet<AssetViewModel>(dirtying.SelectMany(x => x.Dirtiables.OfType<AssetViewModel>()));
        if (dirtiables.Count > 0)
        {
            await session.NotifyAssetPropertiesChangedAsync(dirtiables);
        }
        await Dispatcher.InvokeAsync(() => transactions.Add(new TransactionViewModel(ServiceProvider, e.Transaction)));
    }

    private async void TransactionUndoneOrRedone(object? sender, TransactionEventArgs e)
    {
        var dirtying = e.Transaction.Operations.SelectMany(DirtiableManager.GetDirtyingOperations);
        var dirtiables = new HashSet<AssetViewModel>(dirtying.SelectMany(x => x.Dirtiables.OfType<AssetViewModel>()));
        if (dirtiables.Count > 0)
        {
            await session.NotifyAssetPropertiesChangedAsync(dirtiables);
        }

        await Dispatcher.InvokeAsync(() => transactions.ForEach(x => x.Refresh()));
    }

    private void RefreshUndoRedoStatus()
    {
        UndoCommand.IsEnabled = CanUndo;
        RedoCommand.IsEnabled = CanRedo;
    }
}
