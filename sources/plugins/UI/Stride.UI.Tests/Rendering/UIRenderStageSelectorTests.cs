// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Rendering.UI;

using Xunit;

namespace Stride.UI.Tests.Rendering;

public class UIRenderStageSelectorTests
{
    [Fact]
    public void FullScreenUiUsesAfterPostEffectsStageWhenAvailable()
    {
        var transparentStage = new RenderStage("Transparent", "Main") { Index = 0 };
        var afterPostEffectsStage = new RenderStage("AfterPostEffects", "AfterPostEffects") { Index = 1 };

        var renderObject = CreateRenderObject(isFullScreen: true);

        var selector = new UIRenderStageSelector
        {
            TransparentRenderStage = transparentStage,
            AfterPostEffectsRenderStage = afterPostEffectsStage,
            EffectName = "Test"
        };

        selector.Process(renderObject);

        Assert.False(renderObject.ActiveRenderStages[transparentStage.Index].Active);
        Assert.Equal("Test", renderObject.ActiveRenderStages[afterPostEffectsStage.Index].EffectSelector.EffectName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiWithoutAfterPostEffectsStageUsesTransparentStage(bool isFullScreen)
    {
        var transparentStage = new RenderStage("Transparent", "Main") { Index = 0 };

        var renderObject = CreateRenderObject(isFullScreen);

        var selector = new UIRenderStageSelector
        {
            TransparentRenderStage = transparentStage,
            EffectName = "Test"
        };

        selector.Process(renderObject);

        Assert.Equal("Test", renderObject.ActiveRenderStages[transparentStage.Index].EffectSelector.EffectName);
    }

    private static RenderUIElement CreateRenderObject(bool isFullScreen)
    {
        return new RenderUIElement
        {
            IsFullScreen = isFullScreen,
            ActiveRenderStages = new ActiveRenderStage[2]
        };
    }
}
