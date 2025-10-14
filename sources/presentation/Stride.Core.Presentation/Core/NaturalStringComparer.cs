// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

namespace Stride.Core.Presentation.Core;

public partial class NaturalStringComparer : IComparer<string>
{
    private static readonly Regex NumericRegex = GetNumericRegex();
    private readonly StringComparison comparison;

    public NaturalStringComparer(StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        this.comparison = comparison;
    }

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        // Split strings by numbers
        var splitX = NumericRegex.Split(x.Replace(" ", string.Empty));
        var splitY = NumericRegex.Split(y.Replace(" ", string.Empty));

        var comparer = 0;
        for (var i = 0; comparer == 0 && i < splitX.Length; i++)
        {
            if (splitY.Length <= i)
            {
                // x has more numeric patterns: x > y
                comparer = 1;
                break;
            }

            if (int.TryParse(splitX[i], out var numericX))
            {
                if (int.TryParse(splitY[i], out var numericY))
                {
                    // both numbers
                    comparer = numericX - numericY;
                }
                else
                {
                    // x is a number but y is not
                    comparer = 1;
                }
            }
            else
            {
                // both not numbers, fallback to default string comparison
                comparer = string.Compare(splitX[i], splitY[i], comparison);
            }
        }

        return comparer;
    }

    [GeneratedRegex("([0-9]+)")]
    private static partial Regex GetNumericRegex();
}
