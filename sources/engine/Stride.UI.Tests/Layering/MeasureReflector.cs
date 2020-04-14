// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// Element that returns the size provided during the measure.
    /// Can be used to analyze the size during measure.
    /// </summary>
    public class MeasureReflector: UIElement
    {
        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            return availableSizeWithoutMargins;
        }
    }
}
