// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// An image that an assembly declares with <see cref="TypeImageAttribute"/> or <see cref="EnumImageAttribute"/>, read from
/// its embedded resources and not decoded yet.
/// </summary>
/// <param name="Target">The type, or the enum value, the image is for.</param>
/// <param name="Data">The content of the image file.</param>
public sealed record DeclaredImage(object Target, byte[] Data);
