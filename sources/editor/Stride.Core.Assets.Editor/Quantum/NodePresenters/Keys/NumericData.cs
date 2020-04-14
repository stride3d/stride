// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys
{
    public static class NumericData
    {
        public const string Minimum = nameof(Minimum);
        public const string Maximum = nameof(Maximum);
        public const string DecimalPlaces = nameof(DecimalPlaces);
        public const string LargeStep = nameof(LargeStep);
        public const string SmallStep = nameof(SmallStep);

        public static readonly PropertyKey<object> MinimumKey = new PropertyKey<object>(Minimum, typeof(NumericData));
        public static readonly PropertyKey<object> MaximumKey = new PropertyKey<object>(Maximum, typeof(NumericData));
        public static readonly PropertyKey<int?> DecimalPlacesKey = new PropertyKey<int?>(DecimalPlaces, typeof(NumericData));
        public static readonly PropertyKey<double?> LargeStepKey = new PropertyKey<double?>(LargeStep, typeof(NumericData));
        public static readonly PropertyKey<double?> SmallStepKey = new PropertyKey<double?>(SmallStep, typeof(NumericData));
    }
}
