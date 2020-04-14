// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Presentation.CurveEditor.Views.Behaviors
{
    abstract class AxisBehavior : Behavior<UIElement>
    {
        /// <summary>
        /// Identifies the <see cref="DrawingView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DrawingViewProperty =
            DependencyProperty.Register(nameof(DrawingView), typeof(IDrawingView), typeof(AxisBehavior));
        /// <summary>
        /// Identifies the <see cref="XAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisProperty =
               DependencyProperty.Register(nameof(XAxis), typeof(AxisBase), typeof(AxisBehavior));
        /// <summary>
        /// Identifies the <see cref="YAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisProperty =
               DependencyProperty.Register(nameof(YAxis), typeof(AxisBase), typeof(AxisBehavior));
        /// <summary>
        /// Identifies the <see cref="XModifiers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XModifiersProperty =
               DependencyProperty.Register(nameof(XModifiers), typeof(ModifierKeys), typeof(AxisBehavior), new PropertyMetadata(ModifierKeys.None));
        /// <summary>
        /// Identifies the <see cref="YModifiers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YModifiersProperty =
               DependencyProperty.Register(nameof(YModifiers), typeof(ModifierKeys), typeof(AxisBehavior), new PropertyMetadata(ModifierKeys.None));
        
        public IDrawingView DrawingView { get { return (IDrawingView)GetValue(DrawingViewProperty); } set { SetValue(DrawingViewProperty, value); } }

        public AxisBase XAxis { get { return (AxisBase)GetValue(XAxisProperty); } set { SetValue(XAxisProperty, value); } }

        public AxisBase YAxis { get { return (AxisBase)GetValue(YAxisProperty); } set { SetValue(YAxisProperty, value); } }

        public ModifierKeys XModifiers { get { return (ModifierKeys)GetValue(XModifiersProperty); } set { SetValue(XModifiersProperty, value); } }

        public ModifierKeys YModifiers { get { return (ModifierKeys)GetValue(YModifiersProperty); } set { SetValue(YModifiersProperty, value); } }

        protected bool HasXModifiers()
        {
            return XModifiers == ModifierKeys.None ? Keyboard.Modifiers == ModifierKeys.None : Keyboard.Modifiers.HasFlag(XModifiers);
        }

        protected bool HasYModifiers()
        {
            return YModifiers == ModifierKeys.None ? Keyboard.Modifiers == ModifierKeys.None : Keyboard.Modifiers.HasFlag(YModifiers);
        }
    }
}
