// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Presentation.ViewModel
{
    /// <summary>
    /// An implementation of the <see cref="EditableViewModel"/> that is also itself an <see cref="IDirtiable"/>. The <see cref="Dirtiables"/> 
    /// property returns an enumerable containing the instance itself.
    /// </summary>
    public abstract class DirtiableEditableViewModel : EditableViewModel, IDirtiable
    {
        private bool isDirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirtiableEditableViewModel"/> class.
        /// </summary>
        protected DirtiableEditableViewModel([NotNull] IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <inheritdoc/>
        public bool IsDirty
        {
            get => isDirty;
            private set
            {
                SetValueUncancellable(ref isDirty, value);
                // Note: we always call this method even if the value didn't change as some processes migth depend on it.
                OnDirtyFlagSet();
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<IDirtiable> Dirtiables => this.Yield();

        protected virtual void OnDirtyFlagSet()
        {
            // intentionally do nothing
        }

        protected virtual void UpdateDirtiness(bool value)
        {
            IsDirty = value;
        }

        void IDirtiable.UpdateDirtiness(bool value)
        {
            UpdateDirtiness(value);
        }
    }
}
