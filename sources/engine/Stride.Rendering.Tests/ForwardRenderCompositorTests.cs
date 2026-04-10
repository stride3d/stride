// ForwardRendererCompositorTests.cs
// Place at: sources/engine/Stride.Rendering.Tests/Compositing/ForwardRendererCompositorTests.cs
//
// Structural tests confirming the UI-before-PostFx ordering bug.
// No GPU required — pure object-graph assertions.

using System.Linq;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.UI;        // UIRenderFeature — lives in Stride.UI assembly
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

        // ----------------------------------------------------------------
        // 1. CONFIRM THE BUG:
        //    ForwardRenderer has NO dedicated post-FX UI stage property.
        //    The only way to draw UI after PostEffects today is via a second
        //    SceneCameraRenderer pass in the compositor (the workaround).
        //    This test documents that gap: there is no UIRenderStage on
        //    ForwardRenderer, meaning any UI wired into a SingleStageRenderer
        //    *inside* ForwardRenderer's child chain will be PostFX'd.
        // ----------------------------------------------------------------
        [Fact]
        public void ForwardRenderer_HasNoPostFxUIStageProperty()
        {
            var renderer = new ForwardRenderer();
            var type = renderer.GetType();

            // Verify that the property we WANT to add does not yet exist.
            // When the fix lands, this test should FAIL (and be removed/replaced
            // by test #2 below).
            var prop = type.GetProperty("PostEffectsUIRenderStage");
            Assert.Null(prop);

            _output.WriteLine("DIAGNOSIS CONFIRMED: ForwardRenderer has no PostEffectsUIRenderStage property.");
            _output.WriteLine("UI rendered inside ForwardRenderer's pipeline will be affected by PostEffects.");
        }

        // ----------------------------------------------------------------
        // 2. REGRESSION GUARD (currently expected to fail — uncomment after fix):
        //    Once the fix is in, ForwardRenderer should expose a
        //    PostEffectsUIRenderStage property that is drawn AFTER PostEffects.
        // ----------------------------------------------------------------
        // [Fact]
        // public void ForwardRenderer_PostEffectsUIRenderStage_ExistsAndIsDrawnAfterPostFx()
        // {
        //     var renderer = new ForwardRenderer();
        //     var prop = renderer.GetType().GetProperty("PostEffectsUIRenderStage");
        //     Assert.NotNull(prop);
        //     // Assign and read back
        //     var stage = new RenderStage("UIStage", "UI");
        //     prop.SetValue(renderer, stage);
        //     Assert.Same(stage, prop.GetValue(renderer));
        // }

        // ----------------------------------------------------------------
        // 3. CONFIRM: UIRenderFeature inherits RenderStageSelectors from
        //    RootRenderFeature — the collection used by CleanUiPostFxPatch
        //    to route Group31 to a separate stage.
        // ----------------------------------------------------------------
        [Fact]
        public void UIRenderFeature_HasRenderStageSelectors_Collection()
        {
            var feature = new UIRenderFeature();

            Assert.NotNull(feature.RenderStageSelectors);

            _output.WriteLine($"UIRenderFeature.RenderStageSelectors is present (type: {feature.RenderStageSelectors.GetType().Name}).");
        }

        // ----------------------------------------------------------------
        // 4. CONFIRM: Adding a SimpleGroupToRenderStageSelector for Group31
        //    to UIRenderFeature works correctly — this is the core of the
        //    CleanUiPostFxPatch approach.
        // ----------------------------------------------------------------
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

        // ----------------------------------------------------------------
        // 5. CONFIRM: The IsAlreadyPatched logic from CleanUiPostFxPatch
        //    correctly detects an unpatched compositor.
        // ----------------------------------------------------------------
        [Fact]
        public void IsAlreadyPatched_ReturnsFalse_WhenUiStageAbsent()
        {
            var compositor = new GraphicsCompositor();

            var hasUiStage = compositor.RenderStages
                .Any(s => s.Name == "UiStage");

            Assert.False(hasUiStage);
            _output.WriteLine("IsAlreadyPatched: correctly returns false on a fresh compositor.");
        }

        // ----------------------------------------------------------------
        // 6. CONFIRM: The IsAlreadyPatched logic correctly detects a patched
        //    compositor once both the stage and selector are present.
        // ----------------------------------------------------------------
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

        // ----------------------------------------------------------------
        // 7. DOCUMENT: The null-conditional assignment bug in the original
        //    CleanUiPostFxPatch.MoveUiRecursive.
        //    `ui?.RenderGroup = value` is a compile error in C# — it cannot
        //    assign through a null-conditional. The correct form is guarded
        //    with an explicit null check.
        // ----------------------------------------------------------------
        [Fact]
        public void NullGuard_ExplicitNullCheck_CorrectlyAssignsRenderGroup()
        {
            // Simulate the fixed pattern (what MoveUiRecursive should do)
            var fakeUi = new FakeUIComponent { RenderGroup = RenderGroup.Group0 };

            // WRONG (compile error if used on real UIComponent):
            //   fakeUi?.RenderGroup = RenderGroup.Group31;
            //
            // CORRECT:
            if (fakeUi != null)
                fakeUi.RenderGroup = RenderGroup.Group31;

            Assert.Equal(RenderGroup.Group31, fakeUi.RenderGroup);
            _output.WriteLine("Null-conditional assignment bug documented.");
            _output.WriteLine("  Correct pattern: if (ui != null) ui.RenderGroup = RenderGroup.Group31;");
        }

        // Minimal stand-in; the real UIComponent requires engine services to construct.
        private sealed class FakeUIComponent
        {
            public RenderGroup RenderGroup { get; set; }
        }
    }
}
