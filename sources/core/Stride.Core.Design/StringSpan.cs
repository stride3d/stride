// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Stride.Core;

/// <summary>
/// A region of character in a string.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct StringSpan : IEquatable<StringSpan>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringSpan"/> struct.
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="length">The length.</param>
    public StringSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    /// <summary>
    /// The start offset of the span.
    /// </summary>
    public int Start;

    /// <summary>
    /// The length of the span
    /// </summary>
    public int Length;

    /// <summary>
    /// Gets a value indicating whether this instance is valid (Start greater or equal to 0, and Length greater than 0)
    /// </summary>
    /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
    public readonly bool IsValid
    {
        get
        {
            return Start >= 0 && Length > 0;
        }
    }

    /// <summary>
    /// Gets the next position = Start + Length.
    /// </summary>
    /// <value>The next.</value>
    public readonly int Next => Start + Length;

    /// <summary>
    /// The end offset of the span.
    /// </summary>
    public readonly int End => Start + Length - 1;

    public readonly bool Equals(StringSpan other)
    {
        return Start == other.Start && Length == other.Length;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is StringSpan stringSpan && Equals(stringSpan);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Start, Length);
    }

    public static bool operator ==(StringSpan left, StringSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StringSpan left, StringSpan right)
    {
        return !left.Equals(right);
    }

    public override readonly string ToString()
    {
        return IsValid ? string.Format("[{0}-{1}]", Start, End) : "[N/A]";
    }
}
