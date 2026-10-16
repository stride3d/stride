// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.ViewModel
{
    internal class FixupAssetContext : IDisposable
    {
        private readonly IUndoRedoService undoRedoService;
        private readonly Action<bool> setInContext;

        public FixupAssetContext(IUndoRedoService undoRedoService, Action<bool> setInContext)
        {
            this.undoRedoService = undoRedoService;
            this.setInContext = setInContext;
            setInContext(true);
            // The changes made in the context are recorded asset by asset, with no propagation from a base to its
            // derived assets: they are undone and redone in the context too, else a derived asset gets each change
            // twice, its own and the one propagated from its base. Operations roll back in reverse order.
            PushContextChange(undoInContext: false);
        }

        public void Dispose()
        {
            PushContextChange(undoInContext: true);
            setInContext(false);
        }

        private void PushContextChange(bool undoInContext)
        {
            if (!undoRedoService.TransactionInProgress)
                return;
            undoRedoService.PushOperation(new AnonymousDirtyingOperation([], () => setInContext(undoInContext), () => setInContext(!undoInContext)));
        }
    }
}
