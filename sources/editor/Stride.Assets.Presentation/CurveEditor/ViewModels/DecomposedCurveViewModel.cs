// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Drawing;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
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
