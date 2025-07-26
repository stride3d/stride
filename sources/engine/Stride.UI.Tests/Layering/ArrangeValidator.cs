// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;

using Stride.Core.Mathematics;

namespace Stride.UI.Tests.Layering
{
    class ArrangeValidator : UIElement
    {
        public Size2F ExpectedArrangeValue;
        public Size2F ReturnedMeasuredValue;

        protected override Size2F MeasureOverride(Size2F availableSizeWithoutMargins)
        {
            return ReturnedMeasuredValue;
        }

        protected override Size2F ArrangeOverride(Size2F finalSizeWithoutMargins)
        {
            var maxLength = Math.Max(((Vector2)finalSizeWithoutMargins).Length(), ((Vector2)ExpectedArrangeValue).Length());
            Assert.True(((Vector2)(finalSizeWithoutMargins - ExpectedArrangeValue)).Length() <= maxLength * 0.001f, 
                "Arrange validator test failed: expected value=" + ExpectedArrangeValue + ", Received value=" + finalSizeWithoutMargins + " (Validator='" + Name + "'");

            return base.ArrangeOverride(finalSizeWithoutMargins);
        }
    }
}
