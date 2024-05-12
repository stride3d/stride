// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

#nullable enable

namespace Stride.Core.Assets.Editor.Internal;

internal static class TypeHelpers
{
    /// <summary>
    /// Try to get the type associated with the given <paramref name="keyType"/> from the provided <paramref name="typeMap"/>,
    /// going through the type inheritance hierarchy until a match is found.
    /// </summary>
    /// <param name="keyType">A key to the provided <paramref name="typeMap"/>.</param>
    /// <param name="typeMap">A dictionary mapping types.</param>
    /// <returns>The associated type if found; otherwise, <c>null</c>.</returns>
    public static Type? TryGetTypeOrBase(Type keyType, IReadOnlyDictionary<Type, Type> typeMap)
    {
        var currentType = keyType;
        Type? returnType;
        do
        {
            if (typeMap.TryGetValue(currentType, out returnType))
                break;

            currentType = currentType.BaseType;
        } while (currentType != null);

        return returnType;
    }
}
