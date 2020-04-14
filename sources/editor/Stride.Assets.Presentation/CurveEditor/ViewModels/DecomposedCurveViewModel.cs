// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Drawing;
using Xenko.Animations;

namespace Xenko.Assets.Presentation.CurveEditor.ViewModels
{
    /// <summary>
    /// Represents a curve that is decomposed into several child curves.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public abstract class DecomposedCurveViewModel<TValue> : CurveViewModelBase<TValue>
        where TValue : struct
    {
        protected DecomposedCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] IComputeCurve<TValue> computeCurve, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        public override void Initialize()
        {
            foreach (var child in Children.Cast<CurveViewModelBase<TValue>>())
            {
                child.Initialize();
            }
        }

        public override void Render(IDrawingContext drawingContext, bool isCurrentCurve)
        {
            foreach (var child in Children)
            {
                child.Render(drawingContext, false);
            }
        }
    }
}
