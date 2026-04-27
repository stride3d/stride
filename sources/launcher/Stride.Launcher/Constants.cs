// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Launcher;

internal struct Names
{
    public const string GameStudio = nameof(GameStudio);
    public const string Stride = nameof(Stride);
    public const string Xenko = nameof(Xenko);
}

internal struct GameStudioNames
{
    public const string Stride = $"{Names.Stride}.{Names.GameStudio}";
    public const string StrideAvalonia = $"{Names.Stride}.{Names.GameStudio}.Avalonia.Desktop";
    public const string Xenko = $"{Names.Xenko}.{Names.GameStudio}";
}
