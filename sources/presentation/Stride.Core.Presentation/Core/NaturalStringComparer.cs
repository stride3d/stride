// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Stride.Core.Presentation.Core
{
    public class NaturalStringComparer : IComparer<string>
    {
        private static readonly Regex NumericRegex = new Regex("([0-9]+)", RegexOptions.Compiled);
        private readonly StringComparison comparison;

        public NaturalStringComparer(StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            this.comparison = comparison;
        }

        public int Compare(string x, string y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            
            // Split strings by numbers
            var splitX = NumericRegex.Split(x.Replace(" ", ""));
            var splitY = NumericRegex.Split(y.Replace(" ", ""));

            var comparer = 0;
            for (var i = 0; comparer == 0 && i < splitX.Length; i++)
            {
                if (splitY.Length <= i)
                {
                    // x has more numeric patterns: x > y
                    comparer = 1;
                    break;
                }

                int numericX;
                if (int.TryParse(splitX[i], out numericX))
                {
                    int numericY;
                    if (int.TryParse(splitY[i], out numericY))
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
    }
}
