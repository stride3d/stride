// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.Preview;

public interface ITextureBasePreview
{
    float SpriteScale { get; set; }

    event EventHandler? SpriteScaleChanged;

    IEnumerable<int> GetAvailableMipMaps();

    void DisplayMipMap(int parseMipMapLevel);

    void ZoomIn(Vector2? centerPosition);

    void ZoomOut(Vector2? centerPosition);

    void FitOnScreen();

    void ScaleToRealSize();
}
