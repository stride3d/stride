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
            TestName = nameof(SceneNoLighting);
            RunGameTest(new LightingTests { AmbientLight = false });
        }

        [Fact]
        public void SceneAmbientLight()
        {
            TestName = nameof(SceneAmbientLight);
            RunGameTest(new LightingTests());
        }

        [Fact]
        public void ScenePointLight()
        {
            TestName = nameof(ScenePointLight);
            RunGameTest(new LightingTests { PointLight = true });
        }

        [Fact]
        public void ScenePointLightShadowCubeMap()
        {
            TestName = nameof(ScenePointLightShadowCubeMap);
            RunGameTest(new LightingTests { PointLightShadowCubeMap = true });
        }

        [Fact]
        public void ScenePointLightShadowParaboloid()
        {
            TestName = nameof(ScenePointLightShadowParaboloid);
            RunGameTest(new LightingTests { PointLightShadowParaboloid = true });
        }

        [Fact]
        public void SceneSpotLight()
        {
            TestName = nameof(SceneSpotLight);
            RunGameTest(new LightingTests { SpotLight = true });
        }

        [Fact]
        public void SceneSpotLightShadow()
        {
            TestName = nameof(SceneSpotLightShadow);
            RunGameTest(new LightingTests { SpotLightShadow = true });
        }

        [Fact]
        public void SceneDirectionalLight()
        {
            TestName = nameof(SceneDirectionalLight);
            RunGameTest(new LightingTests { DirectionalLight = true });
        }

        [Fact]
        public void SceneDirectionalLightShadowOneCascade()
        {
            TestName = nameof(SceneDirectionalLightShadowOneCascade);
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true });
        }

        [Fact]
        public void SceneTwoDirectionalLightShadowOneCascade()
        {
            TestName = nameof(SceneTwoDirectionalLightShadowOneCascade);
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, DirectionalLightShadowOneCascade2 = true });
        }

        [Fact]
        public void SceneDirectionalLightShadowOneFourCascade()
        {
            TestName = nameof(SceneDirectionalLightShadowOneFourCascade);
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, DirectionalLightShadowFourCascades = true });
        }

        [Fact]
        public void SceneDirectionalLightShadowOneCascadePCF()
        {
            TestName = nameof(SceneDirectionalLightShadowOneCascadePCF);
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascadePCF = true });
        }

        [Fact]
        public void SceneDirectionalLightShadowFourCascades()
        {
            TestName = nameof(SceneDirectionalLightShadowFourCascades);
            RunGameTest(new LightingTests { DirectionalLightShadowFourCascades = true });
        }

        [Fact]
        public void SceneSkybox()
        {
            TestName = nameof(SceneSkybox);
            RunGameTest(new LightingTests { Skybox = true });
        }

        [Fact]
        public void SceneSkyboxRotated()
        {
            TestName = nameof(SceneSkyboxRotated);
            RunGameTest(new LightingTests { SkyboxRotated = true });
        }

        [Fact]
        public void SceneSkyboxMultiple()
        {
            TestName = nameof(SceneSkyboxMultiple);
            RunGameTest(new LightingTests { Skybox = true, SkyboxRotated = true });
        }

    }
}
