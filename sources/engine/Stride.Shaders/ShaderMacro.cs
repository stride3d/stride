// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Shaders;

[DataContract]
public readonly struct ShaderMacro(string name, object definition) : IEquatable<ShaderMacro>
{
    /// <summary>
    /// Preprocessor macro.
    /// </summary>
    public readonly string Name = name ?? throw new ArgumentNullException(nameof(name));

    public readonly string Definition = definition is not null
                                            ? definition is bool
                                                ? definition.ToString().ToLowerInvariant()
                                                : definition.ToString()
                                            : string.Empty;


    public readonly bool Equals(ShaderMacro other)
    {
        return Equals(other.Name, Name)
            && Equals(other.Definition, Definition);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is null)
            return false;

        return obj is ShaderMacro other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Name, Definition);
    }

    public override readonly string ToString()
    {
        return $"{Name} = {Definition}";
    }

    #region Operators

    public static bool operator ==(ShaderMacro left, ShaderMacro right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ShaderMacro left, ShaderMacro right)
    {
        return !(left == right);
    }

    #endregion
}
