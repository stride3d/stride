// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Transactions;

namespace Stride.Assets.Editor.ViewModels.SpriteEditor;

/// <summary>
/// A part of a sprite that the handles of the view resize or move. A drag is one entry of the undo history, and the part
/// follows the zoom of the editor (<see cref="ScaleFactor"/>) to turn the changes in the view into pixels of the image.
/// </summary>
public abstract class ResizableSpriteInfoPartViewModel : SpriteInfoPartViewModel, IResizingTarget
{
    private ITransaction? resizingTransaction;
    private double scaleFactor;

    protected ResizableSpriteInfoPartViewModel(SpriteInfoViewModel sprite)
        : base(sprite)
    {
        Sprite.Editor.Viewport.PropertyChanged += ViewportPropertyChanged;
        scaleFactor = sprite.Editor.Viewport.ScaleFactor;
    }

    /// <summary>
    /// Gets or sets the zoom of the view: the size of a pixel of the image in the view.
    /// </summary>
    public double ScaleFactor { get => scaleFactor; set => SetValue(ref scaleFactor, value); }

    /// <inheritdoc />
    public override void Destroy()
    {
        Sprite.Editor.Viewport.PropertyChanged -= ViewportPropertyChanged;
        base.Destroy();
    }

    protected abstract void OnResizeDelta(ResizingDirection direction, double horizontalChange, double verticalChange);

    protected abstract string? ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange);

    private void ViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var viewport = (ViewportViewModel)sender!;
        if (e.PropertyName == nameof(ViewportViewModel.ScaleFactor))
            ScaleFactor = viewport.ScaleFactor;
    }

    void IResizingTarget.OnResizingStarted(ResizingDirection direction)
    {
        if (resizingTransaction != null)
            throw new InvalidOperationException("A resize operation was already in progress.");

        resizingTransaction = UndoRedoService.CreateTransaction();
    }

    void IResizingTarget.OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        if (resizingTransaction == null)
            throw new InvalidOperationException("No resize operation in progress.");

        // The synchronization context has changed here, we must signal we intentionally continue an existing transaction.
        resizingTransaction.Continue();
        OnResizeDelta(direction, horizontalChange, verticalChange);
    }

    void IResizingTarget.OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        if (resizingTransaction == null)
            throw new InvalidOperationException("No resize operation in progress.");

        // The synchronization context has changed here, we must signal we intentionally continue an existing transaction.
        resizingTransaction.Continue();
        var transactionName = ComputeTransactionName(direction, horizontalChange, verticalChange);
        if (transactionName != null)
            UndoRedoService.SetName(resizingTransaction, transactionName);
        resizingTransaction.Complete();

        resizingTransaction = null;
    }
}
