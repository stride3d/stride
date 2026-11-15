// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Assets
{
    /// <summary>
    /// The editor's previews do not compile assets of the attributed type as dependencies of the asset they show, for a
    /// type whose compilation needs what previews never compile (a navigation mesh is built from a whole scene).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ExcludedFromPreviewsAttribute : Attribute
    {
    }
}
