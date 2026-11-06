// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

internal static class SpriteFontData
{
    public const string SpriteFontAvailableFonts = nameof(SpriteFontAvailableFonts);
    public static readonly PropertyKey<IEnumerable<string>> Key = new(SpriteFontAvailableFonts, typeof(SpriteFontData));
}
