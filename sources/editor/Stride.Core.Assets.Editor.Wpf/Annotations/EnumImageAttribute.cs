// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Assets.Editor.Annotations;

/// <summary>
/// Declares the editor image of an enum value, an embedded resource of the plugin assembly.
/// <see cref="ResourceName"/> is the full manifest resource name or its end, such as <c>Play.png</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class EnumImageAttribute : Attribute
{
    public EnumImageAttribute(object value, string resourceName)
    {
        Value = value;
        ResourceName = resourceName;
    }

    /// <summary>
    /// The enum value the image stands for.
    /// </summary>
    public object Value { get; }

    public string ResourceName { get; }
}
