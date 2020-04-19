// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xunit;

using Stride.Core.Mathematics;

namespace Stride.UI.Tests.Layering
{
    class MeasureValidator : UIElement
    {
        public Vector3 ReturnedMeasuredValue;
        public Vector3 ExpectedMeasureValue;

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            for (int i = 0; i < 3; i++)
            {
                var val1 = availableSizeWithoutMargins[i];
                var val2 = ExpectedMeasureValue[i];

                if (val1 == val2) continue; // value can be infinity

                var maxLength = Math.Max(Math.Abs(val1), Math.Abs(val2));
                Assert.True(Math.Abs(val1 - val2) < maxLength * 0.001f,
                    "Measure validator test failed: expected value=" + ExpectedMeasureValue + ", Received value=" + availableSizeWithoutMargins + " (Validator='" + Name + "'");
            }

            return ReturnedMeasuredValue;
        }
    }
}
