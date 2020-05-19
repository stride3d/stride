// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Engine;
using Stride.Graphics.Regression;
using Stride.Rendering.Compositing;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Test lighting and shadows.
    /// </summary>
    /// Note: shadows stabilization is still not working, new golds will be needed when fixed.
    public class LightingTests : GameTestBase
    {
        public Action<LightingTests> SetupLighting { get; set; }

        public bool AmbientLight { get; set; } = true;

        public bool PointLight { get; set; }

        public bool PointLightShadowCubeMap { get; set; }

        public bool PointLightShadowParaboloid { get; set; }

        public bool SpotLight { get; set; }

        public bool SpotLightShadow { get; set; }

        public bool DirectionalLight { get; set; }

        public bool DirectionalLightShadowOneCascade { get; set; }

        public bool DirectionalLightShadowOneCascade2 { get; set; }

        public bool DirectionalLightShadowFourCascades { get; set; }

        public bool DirectionalLightShadowOneCascadePCF { get; set; }

        public bool Skybox { get; set; }

        public bool SkyboxRotated { get; set; }

        public LightingTests()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();

            // Override initial scene
            SceneSystem.InitialSceneUrl = "LightingTests/LightingScene";
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Load default graphics compositor
            SceneSystem.GraphicsCompositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");

            // Setup camera script
            var camera = SceneSystem.SceneInstance.First(x => x.Name == "Camera");
            if (camera != null)
            {
                var cameraScript = new FpsTestCamera();
                camera.Add(cameraScript);
            }

            SetupLighting?.Invoke(this);

            // Setup lights
            SceneSystem.SceneInstance.First(x => x.Name == nameof(AmbientLight)).Get<LightComponent>().Enabled = AmbientLight;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(PointLight)).Get<LightComponent>().Enabled = PointLight;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(PointLightShadowCubeMap)).Get<LightComponent>().Enabled = PointLightShadowCubeMap;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(PointLightShadowParaboloid)).Get<LightComponent>().Enabled = PointLightShadowParaboloid;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(SpotLight)).Get<LightComponent>().Enabled = SpotLight;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(SpotLightShadow)).Get<LightComponent>().Enabled = SpotLightShadow;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(DirectionalLight)).Get<LightComponent>().Enabled = DirectionalLight;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(DirectionalLightShadowOneCascade)).Get<LightComponent>().Enabled = DirectionalLightShadowOneCascade;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(DirectionalLightShadowOneCascade2)).Get<LightComponent>().Enabled = DirectionalLightShadowOneCascade2;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(DirectionalLightShadowFourCascades)).Get<LightComponent>().Enabled = DirectionalLightShadowFourCascades;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(DirectionalLightShadowOneCascadePCF)).Get<LightComponent>().Enabled = DirectionalLightShadowOneCascadePCF;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(Skybox)).Get<LightComponent>().Enabled = Skybox;
            SceneSystem.SceneInstance.First(x => x.Name == nameof(SkyboxRotated)).Get<LightComponent>().Enabled = SkyboxRotated;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // Take screenshot first frame
            FrameGameSystem.TakeScreenshot();
        }

        [Fact]
        public void SceneNoLighting()
        {
            RunGameTest(new LightingTests { AmbientLight = false, TestName = nameof(SceneNoLighting) });
        }

        [Fact]
        public void SceneAmbientLight()
        {
            RunGameTest(new LightingTests { TestName = nameof(SceneAmbientLight) });
        }

        [Fact]
        public void ScenePointLight()
        {
            RunGameTest(new LightingTests { PointLight = true, TestName = nameof(ScenePointLight) });
        }

        [Fact]
        public void ScenePointLightShadowCubeMap()
        {
            RunGameTest(new LightingTests { PointLightShadowCubeMap = true, TestName = nameof(ScenePointLightShadowCubeMap) });
        }

        [Fact]
        public void ScenePointLightShadowParaboloid()
        {
            RunGameTest(new LightingTests { PointLightShadowParaboloid = true, TestName = nameof(ScenePointLightShadowParaboloid) });
        }

        [Fact]
        public void SceneSpotLight()
        {
            RunGameTest(new LightingTests { SpotLight = true, TestName = nameof(SceneSpotLight) });
        }

        [Fact]
        public void SceneSpotLightShadow()
        {
            RunGameTest(new LightingTests { SpotLightShadow = true, TestName = nameof(SceneSpotLightShadow) });
        }

        [Fact]
        public void SceneDirectionalLight()
        {
            RunGameTest(new LightingTests { DirectionalLight = true, TestName = nameof(SceneDirectionalLight) });
        }

        [Fact]
        public void SceneDirectionalLightShadowOneCascade()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, TestName = nameof(SceneDirectionalLightShadowOneCascade) });
        }

        [Fact]
        public void SceneTwoDirectionalLightShadowOneCascade()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, DirectionalLightShadowOneCascade2 = true, TestName = nameof(SceneTwoDirectionalLightShadowOneCascade) });
        }

        [Fact]
        public void SceneDirectionalLightShadowOneFourCascade()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, DirectionalLightShadowFourCascades = true, TestName = nameof(SceneDirectionalLightShadowOneFourCascade) });
        }

        [Fact]
        public void SceneDirectionalLightShadowOneCascadePCF()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascadePCF = true, TestName = nameof(SceneDirectionalLightShadowOneCascadePCF) });
        }

        [Fact]
        public void SceneDirectionalLightShadowFourCascades()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowFourCascades = true, TestName = nameof(SceneDirectionalLightShadowFourCascades) });
        }

        [Fact]
        public void SceneSkybox()
        {
            RunGameTest(new LightingTests { Skybox = true, TestName = nameof(SceneSkybox) });
        }

        [Fact]
        public void SceneSkyboxRotated()
        {
            RunGameTest(new LightingTests { SkyboxRotated = true, TestName = nameof(SceneSkyboxRotated) });
        }

        [Fact]
        public void SceneSkyboxMultiple()
        {
            RunGameTest(new LightingTests { Skybox = true, SkyboxRotated = true, TestName = nameof(SceneSkyboxMultiple) });
        }

    }
}
