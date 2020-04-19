// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Stride.Assets.Presentation.CurveEditor.Views
{
    /// <summary>
    /// Interaction logic for CurveEditorView.xaml
    /// </summary>
    public partial class CurveEditorView : UserControl
    {
        private static readonly DependencyPropertyKey LastRightClickPositionPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(LastRightClickPosition), typeof(Point), typeof(CurveEditorView), new PropertyMetadata());

        internal static readonly DependencyProperty LastRightClickPositionProperty = LastRightClickPositionPropertyKey.DependencyProperty;

        public CurveEditorView()
        {
            InitializeComponent();
            // Ensure we can give the focus to the editor
            Focusable = true;
        }

        internal Point LastRightClickPosition { get { return (Point)GetValue(LastRightClickPositionProperty); } set { SetValue(LastRightClickPositionPropertyKey, value); } }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            // We give the focus to the editor so shortcuts will work
            if (!IsKeyboardFocusWithin && !(e.OriginalSource is HwndHost))
            {
                Keyboard.Focus(this);
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            LastRightClickPosition = Mouse.GetPosition(CanvasView);
        }
    }
}
