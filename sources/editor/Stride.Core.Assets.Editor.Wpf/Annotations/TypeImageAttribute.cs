// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Assets.Editor.Annotations;

/// <summary>
/// Declares the editor image of <see cref="Type"/>, an embedded resource of the plugin assembly.
/// <see cref="ResourceName"/> is the full manifest resource name or its end, such as <c>VideoComponent.png</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeImageAttribute : Attribute
{
    public TypeImageAttribute(Type type, string resourceName)
    {
        Type = type;
        ResourceName = resourceName;
    }

    public Type Type { get; }

    public string ResourceName { get; }
}
