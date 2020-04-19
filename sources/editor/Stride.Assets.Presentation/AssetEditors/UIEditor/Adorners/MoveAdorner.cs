// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Forms;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Game;
using Stride.Assets.Presentation.ViewModel;
using Stride.UI;
using Stride.UI.Controls;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners
{
    /// <summary>
    /// Represents an adorner that can move the associated  <see cref="UIElement"/>.
    /// </summary>
    internal sealed class MoveAdorner : BorderAdorner, IResizingAdorner
    {
        public MoveAdorner(UIEditorGameAdornerService service, UIElement gameSideElement)
            : base(service, gameSideElement)
        {
            Visual.Name = "[Move]";
            Visual.CanBeHitByUser = true;
        }

        public ResizingDirection ResizingDirection => ResizingDirection.Center;

        public Cursor GetCursor() => CannotMove() ? Cursors.No : Cursors.SizeAll;

        public override void Update(Vector3 position)
        {
            UpdateFromSettings();
            Size = GameSideElement.RenderSize;
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            Visual.Margin = Thickness.UniformCuboid(-BorderThickness);
        }

        private bool CannotMove()
        {
            // If the parent of the associated element is a ContentControl, then moving is disabled
            return GameSideElement.VisualParent is ContentControl;
        }

        private void UpdateFromSettings()
        {
            var editor = Service.Controller.Editor;

            BackgroundColor = editor.SelectionColor*0.2f;
            BorderColor = editor.SelectionColor;
            BorderThickness = editor.SelectionThickness;
        }

        void IResizingAdorner.OnResizingDelta(float horizontalChange, float verticalChange)
        {
            // Nothing to do
        }

        void IResizingAdorner.OnResizingCompleted()
        {
            // Nothing to do
        }
    }
}
