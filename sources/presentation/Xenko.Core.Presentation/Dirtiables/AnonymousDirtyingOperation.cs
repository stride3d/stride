// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Dirtiables
{
    public class AnonymousDirtyingOperation : DirtyingOperation
    {
        private Action undo;
        private Action redo;

        public AnonymousDirtyingOperation([NotNull] IEnumerable<IDirtiable> dirtiables, Action undo, Action redo)
            : base(dirtiables)
        {
            this.undo = undo;
            this.redo = redo;
        }

        /// <inheritdoc/>
        protected override void FreezeContent()
        {
            undo = null;
            redo = null;
        }

        /// <inheritdoc/>
        protected override void Undo()
        {
            undo?.Invoke();
        }

        /// <inheritdoc/>
        protected override void Redo()
        {
            redo?.Invoke();
        }
    }
}
