// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using System.Threading;
using Stride.Core.Transactions;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    public abstract class ResizableSpriteInfoPartViewModel : SpriteInfoPartViewModel, IResizingTarget
    {
        private ITransaction resizingTransaction;
        private SynchronizationContext resizingContext;
        private double scaleFactor;

        protected ResizableSpriteInfoPartViewModel(SpriteInfoViewModel sprite)
            : base(sprite)
        {
            Sprite.Editor.Viewport.PropertyChanged += ViewportPropertyChanged;
            scaleFactor = sprite.Editor.Viewport.ScaleFactor;
        }

        public double ScaleFactor { get { return scaleFactor; } set { SetValue(ref scaleFactor, value); } }

        /// <inheritdoc />
        public override void Destroy()
        {
            Sprite.Editor.Viewport.PropertyChanged -= ViewportPropertyChanged;
            base.Destroy();
        }

        protected abstract void OnResizeDelta(ResizingDirection direction, double horizontalChange, double verticalChange);

        protected abstract string ComputeTransactionName(ResizingDirection direction, double horizontalChange, double verticalChange);

        private void ViewportPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var viewport = (ViewportViewModel)sender;
            if (e.PropertyName == nameof(ViewportViewModel.ScaleFactor))
                ScaleFactor = viewport.ScaleFactor;
        }

        void IResizingTarget.OnResizingStarted(ResizingDirection direction)
        {
            if (resizingTransaction != null || resizingContext != null)
                throw new InvalidOperationException("A resize operation was already in progress.");

            resizingTransaction = UndoRedoService.CreateTransaction();
            resizingContext = SynchronizationContext.Current;
        }

        void IResizingTarget.OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            if (resizingTransaction == null || resizingContext == null)
                throw new InvalidOperationException("No resize operation in progress.");

            // The synchronization context has changed here, we must signal we intentionnally continue an existing transaction.
            resizingTransaction.Continue();
            OnResizeDelta(direction, horizontalChange, verticalChange);
        }

        void IResizingTarget.OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            if (resizingTransaction == null || resizingContext == null)
                throw new InvalidOperationException("No resize operation in progress.");

            // The synchronization context has changed here, we must signal we intentionnally continue an existing transaction.
            resizingTransaction.Continue();
            var transactionName = ComputeTransactionName(direction, horizontalChange, verticalChange);
            UndoRedoService.SetName(resizingTransaction, transactionName);
            resizingTransaction.Complete();

            resizingTransaction = null;
            resizingContext = null;
        }
    }
}
