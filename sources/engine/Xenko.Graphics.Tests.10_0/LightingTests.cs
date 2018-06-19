// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xenko.Engine;
using Xenko.Graphics.Regression;
using Xenko.Rendering.Compositing;

namespace Xenko.Graphics.Tests
{
    /// <summary>
    /// Test lighting and shadows.
    /// </summary>
    /// Note: shadows stabilization is still not working, new golds will be needed when fixed.
    [TestFixture]
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
            CurrentVersion = 20; // Removed incorrect normal anti-aliasing
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

        [Test]
        public void SceneNoLighting()
        {
            RunGameTest(new LightingTests { AmbientLight = false });
        }

        [Test]
        public void SceneAmbientLight()
        {
            RunGameTest(new LightingTests());
        }

        [Test]
        public void ScenePointLight()
        {
            RunGameTest(new LightingTests { PointLight = true });
        }

        [Test]
        public void ScenePointLightShadowCubeMap()
        {
            RunGameTest(new LightingTests { PointLightShadowCubeMap = true });
        }

        [Test]
        public void ScenePointLightShadowParaboloid()
        {
            RunGameTest(new LightingTests { PointLightShadowParaboloid = true });
        }

        [Test]
        public void SceneSpotLight()
        {
            RunGameTest(new LightingTests { SpotLight = true });
        }

        [Test]
        public void SceneSpotLightShadow()
        {
            RunGameTest(new LightingTests { SpotLightShadow = true });
        }

        [Test]
        public void SceneDirectionalLight()
        {
            RunGameTest(new LightingTests { DirectionalLight = true });
        }

        [Test]
        public void SceneDirectionalLightShadowOneCascade()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true });
        }

        [Test]
        public void SceneTwoDirectionalLightShadowOneCascade()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, DirectionalLightShadowOneCascade2 = true });
        }

        [Test]
        public void SceneDirectionalLightShadowOneFourCascade()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascade = true, DirectionalLightShadowFourCascades = true });
        }

        [Test]
        public void SceneDirectionalLightShadowOneCascadePCF()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowOneCascadePCF = true });
        }

        [Test]
        public void SceneDirectionalLightShadowFourCascades()
        {
            RunGameTest(new LightingTests { DirectionalLightShadowFourCascades = true });
        }

        [Test]
        public void SceneSkybox()
        {
            RunGameTest(new LightingTests { Skybox = true });
        }

        [Test]
        public void SceneSkyboxRotated()
        {
            RunGameTest(new LightingTests { SkyboxRotated = true });
        }

        [Test]
        public void SceneSkyboxMultiple()
        {
            RunGameTest(new LightingTests { Skybox = true, SkyboxRotated = true });
        }

    }
}
