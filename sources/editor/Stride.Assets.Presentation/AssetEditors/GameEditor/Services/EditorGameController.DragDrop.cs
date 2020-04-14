// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Stride.Core.Assets.Editor.View.Behaviors;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    partial class EditorGameController<TEditorGame>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DisableDrop()
        {
            GameForm.AllowDrop = false;
        }

        /// <summary>
        /// Initiates a drag-and-drop operation.
        /// </summary>
        /// <param name="items">The items being dragged.</param>
        /// <returns>A value from the <see cref="DragDropEffects"/> enumeration that represents the final effect that was performed during the drag-and-drop operation.</returns>
        protected internal DragDropEffects DoDragDrop(params object[] items)
        {
            var data = new DragContainer(items);
            try
            {
                return GameForm.DoDragDrop(new DataObject(DragContainer.Format, data), DragDropEffects.All);
            }
            catch (COMException)
            {
                return DragDropEffects.None;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EnableDrop()
        {
            GameForm.AllowDrop = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="Control.DragDrop"/>
        protected virtual void OnDragDrop(object sender, DragEventArgs e)
        {
            // Nothing by default
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="Control.DragEnter"/>
        protected virtual void OnDragEnter(object sender, DragEventArgs e)
        {
            // Nothing by default
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="Control.DragLeave"/>
        protected virtual void OnDragLeave(object sender, EventArgs e)
        {
            // Nothing by default
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="Control.DragOver"/>
        protected virtual void OnDragOver(object sender, DragEventArgs e)
        {
            // Nothing by default
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <seealso cref="Control.QueryContinueDrag"/>
        protected virtual void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Nothing by default
        }

        partial void RegisterToDragDropEvents()
        {
            GameForm.DragDrop += OnDragDrop;
            GameForm.DragEnter += OnDragEnter;
            GameForm.DragLeave += OnDragLeave;
            GameForm.DragOver += OnDragOver;
            GameForm.QueryContinueDrag += OnQueryContinueDrag;
        }

        partial void UnregisterFromDragDropEvents()
        {
            GameForm.QueryContinueDrag -= OnQueryContinueDrag;
            GameForm.DragOver -= OnDragOver;
            GameForm.DragLeave -= OnDragLeave;
            GameForm.DragEnter -= OnDragEnter;
            GameForm.DragDrop -= OnDragDrop;
        }
    }
}
