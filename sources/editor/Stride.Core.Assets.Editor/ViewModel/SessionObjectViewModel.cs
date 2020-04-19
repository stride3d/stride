// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class SessionObjectViewModel : DirtiableEditableViewModel, IAddChildViewModel, IIsEditableViewModel, ISessionObjectViewModel
    {
        private bool isEditing;
        private bool isDeleted = true;

        protected SessionObjectViewModel([NotNull] SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            Session = session;
        }

        public abstract string Name { get; set; }

        /// <summary>
        /// Gets the session in which this object is currently in.
        /// </summary>
        [NotNull]
        public SessionViewModel Session { get; }

        /// <summary>
        /// Gets whether this object is currently deleted.
        /// </summary>
        public bool IsDeleted { get { return isDeleted; } set { SetValue(ref isDeleted, value, UpdateIsDeletedStatus); } }

        /// <summary>
        /// Gets whether this object is editable.
        /// </summary>
        public abstract bool IsEditable { get; }

        /// <summary>
        /// Gets or sets whether this package is being edited in the view.
        /// </summary>
        public virtual bool IsEditing { get { return isEditing; } set { if (IsEditable) SetValueUncancellable(ref isEditing, value); } }

        /// <summary>
        /// Gets the display name of the type of this <see cref="SessionObjectViewModel"/>.
        /// </summary>
        public abstract string TypeDisplayName { get; }

        /// <summary>
        /// Gets the <see cref="ThumbnailData"/> associated to this <see cref="SessionObjectViewModel"/>.
        /// </summary>
        ThumbnailData ISessionObjectViewModel.ThumbnailData { get; } = null;

        /// <summary>
        /// Marks this view model as undeleted.
        /// </summary>
        /// <param name="canUndoRedoCreation">Indicates whether a transaction should be created when doing this operation.</param>
        /// <remarks>
        /// This method is intended to be called from constructors, to allow the creation of this view model
        /// to be undoable or not.
        /// </remarks>
        protected void InitialUndelete(bool canUndoRedoCreation)
        {
            if (canUndoRedoCreation)
            {
                SetValue(ref isDeleted, false, UpdateIsDeletedStatus, nameof(IsDeleted));
            }
            else
            {
                SetValueUncancellable(ref isDeleted, false, UpdateIsDeletedStatus, nameof(IsDeleted));
            }
        }

        /// <summary>
        /// Updates related session objects when the <see cref="IsDeleted"/> property changes.
        /// </summary>
        protected abstract void UpdateIsDeletedStatus();

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            message = DragDropBehavior.InvalidDropAreaMessage;
            return false;
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            // Intentionally do nothing.
        }

        protected virtual bool IsValidName(string value, out string error)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (value.Length > 240)
            {
                error = Tr._p("Message", "The name is too long.");
                return false;
            }

            if (value.Contains(UPath.DirectorySeparatorChar) || value.Contains(UPath.DirectorySeparatorCharAlt) || !UPath.IsValid(value))
            {
                error = Tr._p("Message", "The name contains invalid characters.");
                return false;
            }

            if (string.IsNullOrEmpty(value))
            {
                error = Tr._p("Message", "The name is empty.");
                return false;
            }

            error = null;

            return true;
        }
    }
}
