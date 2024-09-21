// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

public static class NumericData
{
    public const string Minimum = nameof(Minimum);
    public const string Maximum = nameof(Maximum);
    public const string DecimalPlaces = nameof(DecimalPlaces);
    public const string LargeStep = nameof(LargeStep);
    public const string SmallStep = nameof(SmallStep);

    public static readonly PropertyKey<object> MinimumKey = new(Minimum, typeof(NumericData));
    public static readonly PropertyKey<object> MaximumKey = new(Maximum, typeof(NumericData));
    public static readonly PropertyKey<int?> DecimalPlacesKey = new(DecimalPlaces, typeof(NumericData));
    public static readonly PropertyKey<double?> LargeStepKey = new(LargeStep, typeof(NumericData));
    public static readonly PropertyKey<double?> SmallStepKey = new(SmallStep, typeof(NumericData));
}
