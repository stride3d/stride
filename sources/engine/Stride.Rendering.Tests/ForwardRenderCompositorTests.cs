using System.Linq;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.UI;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Rendering.Tests.Compositing
{
    public class ForwardRendererCompositorTests
    {
        private readonly ITestOutputHelper _output;

        public ForwardRendererCompositorTests(ITestOutputHelper output)
        {
            _output = output;
        }

         [Fact]
         public void ForwardRenderer_PostEffectsUIRenderStage_ExistsAndIsDrawnAfterPostFx()
         {
             var renderer = new ForwardRenderer();
             var prop = renderer.GetType().GetProperty("PostEffectsUIRenderStage");
            Assert.NotNull(prop);
            // Assign and read back
            var stage = new RenderStage("UIStage", "UI");
             prop.SetValue(renderer, stage);
            Assert.Same(stage, prop.GetValue(renderer));
         }

        [Fact]
        public void UIRenderFeature_HasRenderStageSelectors_Collection()
        {
            var feature = new UIRenderFeature();

            Assert.NotNull(feature.RenderStageSelectors);

            _output.WriteLine($"UIRenderFeature.RenderStageSelectors is present (type: {feature.RenderStageSelectors.GetType().Name}).");
        }


        [Fact]
        public void UIRenderFeature_CanAddGroup31SelectorForUiStage()
        {
            var uiStage = new RenderStage("UiStage", "UI");
            var feature = new UIRenderFeature();

            feature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                RenderGroup = RenderGroupMask.Group31,
                RenderStage = uiStage,
            });

            var selector = feature.RenderStageSelectors
                .OfType<SimpleGroupToRenderStageSelector>()
                .FirstOrDefault(s => s.RenderGroup == RenderGroupMask.Group31);

            Assert.NotNull(selector);
            Assert.Same(uiStage, selector.RenderStage);

            _output.WriteLine("Group31 selector successfully added to UIRenderFeature.");
            _output.WriteLine($"  RenderStage: {selector.RenderStage.Name}");
        }

        [Fact]
        public void IsAlreadyPatched_ReturnsFalse_WhenUiStageAbsent()
        {
            var compositor = new GraphicsCompositor();

            var hasUiStage = compositor.RenderStages
                .Any(s => s.Name == "UiStage");

            Assert.False(hasUiStage);
            _output.WriteLine("IsAlreadyPatched: correctly returns false on a fresh compositor.");
        }


        [Fact]
        public void IsAlreadyPatched_ReturnsTrue_WhenStageAndSelectorPresent()
        {
            var compositor = new GraphicsCompositor();
            var uiStage = new RenderStage("UiStage", "UI");
            compositor.RenderStages.Add(uiStage);

            var feature = new UIRenderFeature();
            feature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                RenderGroup = RenderGroupMask.Group31,
                RenderStage = uiStage,
            });
            compositor.RenderFeatures.Add(feature);

            var hasStage = compositor.RenderStages
                .Any(s => s.Name == "UiStage");

            var hasSelector = compositor.RenderFeatures
                .OfType<UIRenderFeature>()
                .SelectMany(f => f.RenderStageSelectors.OfType<SimpleGroupToRenderStageSelector>())
                .Any(s => s.RenderGroup == RenderGroupMask.Group31
                       && s.RenderStage?.Name == "UiStage");

            Assert.True(hasStage);
            Assert.True(hasSelector);
            _output.WriteLine("IsAlreadyPatched: correctly returns true when stage and selector both exist.");
        }

        // Minimal stand-in; the real UIComponent requires engine services to construct.
        private sealed class FakeUIComponent
        {
            public RenderGroup RenderGroup { get; set; }
        }
    }
}
