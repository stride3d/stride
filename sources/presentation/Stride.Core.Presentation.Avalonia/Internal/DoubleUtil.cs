// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/)
// Licensed under MIT license, courtesy of The .NET Foundation.

namespace Stride.Core.Presentation.Avalonia.Internal;

// ReSharper disable CompareOfFloatsByEqualityOperator

/// <summary>
/// This class provides "fuzzy" comparison functionalities for doubles.
/// </summary>
internal static class DoubleUtil
{
    internal const double Epsilon = 1e-6;

    public static bool AreClose(double a, double b)
    {
        if (a == b) return true;

        return (a - b) is < Epsilon and > -Epsilon;
    }

    public static bool LessThan(double a, double b)
    {
        return (a < b) && !AreClose(a, b);
    }
}
