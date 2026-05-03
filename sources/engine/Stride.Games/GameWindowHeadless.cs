// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games;

/// <summary>
///   A game window implementation for headless (windowless) operation.
///   Runs the game loop without creating any OS window or display surface.
/// </summary>
internal class GameWindowHeadless : GameWindow<object>
{
    private int width;
    private int height;

    public override bool AllowUserResizing { get; set; }

    public override Rectangle ClientBounds => new(0, 0, width, height);

    public override DisplayOrientation CurrentOrientation => DisplayOrientation.Default;

    public override bool IsMinimized => false;

    public override bool Focused => true;

    public override bool IsMouseVisible { get; set; }

    public override WindowHandle NativeWindow => null;

    public override bool Visible { get; set; }

    public override double Opacity { get; set; } = 1.0;

    public override bool IsBorderLess { get; set; }

    public override void BeginScreenDeviceChange(bool willBeFullScreen) { }

    public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
    {
        width = clientWidth;
        height = clientHeight;
    }

    protected internal override void SetSupportedOrientations(DisplayOrientation orientations) { }

    protected override void SetTitle(string title) { }

    internal override void Resize(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
    }

    protected override void Initialize(GameContext<object> gameContext)
    {
        width = gameContext.RequestedWidth > 0 ? gameContext.RequestedWidth : 800;
        height = gameContext.RequestedHeight > 0 ? gameContext.RequestedHeight : 600;
    }

    internal override void Run()
    {
        InitCallback?.Invoke();

        while (!Exiting)
        {
            RunCallback?.Invoke();
        }

        ExitCallback?.Invoke();
    }
}
