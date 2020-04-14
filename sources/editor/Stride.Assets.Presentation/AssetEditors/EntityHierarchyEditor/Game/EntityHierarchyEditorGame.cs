// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Yaml;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Extensions;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.UI;
using Stride.Shaders;
using Stride.Shaders.Compiler;
using StrideEffects;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public static class EditorGraphicsCompositorHelper
    {
        public const string EditorForwardShadingEffect = "StrideEditorForwardShadingEffect";
        public const string EditorHighlightingEffect = "StrideEditorHighlightingEffect";
    }

    public abstract class EntityHierarchyEditorGame : EditorServiceGame
    {
        private readonly TaskCompletionSource<bool> gameContentLoadedTaskSource;
        private readonly IEffectCompiler effectCompiler;
        private readonly string effectLogPath;

        private readonly Vector3 upAxis = Vector3.UnitY;
        private Material fallbackColorMaterial;
        private Material fallbackTextureMaterial;

        protected EntityHierarchyEditorGame(TaskCompletionSource<bool> gameContentLoadedTaskSource, IEffectCompiler effectCompiler, string effectLogPath)
        {
            this.gameContentLoadedTaskSource = gameContentLoadedTaskSource;
            this.effectCompiler = effectCompiler;
            this.effectLogPath = effectLogPath;
            CreateScenePipeline();
        }

        /// <summary>
        /// Gets the actual <see cref="Scene"/> being edited.
        /// </summary>
        // FIXME: turn internal or private/protected
        public Scene ContentScene { get; private set; }

        /// <summary>
        /// Gets the scene in which editor elements can be added.
        /// </summary>
        public Scene EditorScene { get; private set; }

        /// <summary>
        /// The <see cref="SceneSystem"/> for <see cref="EditorScene"/>.
        /// </summary>
        public SceneSystem EditorSceneSystem { get; private set; }

        /// <summary>
        /// Finds the entity identified by <paramref name="entityId"/>.
        /// </summary>
        /// <param name="sceneId">The identifier of the scene containing the entity.</param>
        /// <param name="entityId">The identifier of the entity.</param>
        /// <returns>The entity identified by <paramref name="entityId"/> if it exists; otherwise <c>null</c>.</returns>
        public abstract Entity FindSubEntity(Guid sceneId, Guid entityId);

        /// <inheritdoc />
        public override Vector3 GetPositionInScene(Vector2 mousePosition)
        {
            const float limitAngle = 7.5f * MathUtil.Pi / 180f;
            const float randomDistance = 20f;

            Vector3 scenePosition;
            var cameraService = EditorServices.Get<IEditorGameCameraService>();

            var ray = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, mousePosition, Matrix.Invert(cameraService.ViewMatrix));
            var plane = new Plane(Vector3.Zero, upAxis);

            // Ensures a ray angle with projection plane of at least 'limitAngle' to avoid the object to go to infinity.
            var dotProductValue = Vector3.Dot(ray.Direction, plane.Normal);
            var comparisonSign = Math.Sign(Vector3.Dot(ray.Position, plane.Normal) + plane.D);
            if (comparisonSign * (Math.Acos(dotProductValue) - MathUtil.PiOverTwo) < limitAngle || !plane.Intersects(ref ray, out scenePosition))
                scenePosition = ray.Position + randomDistance * ray.Direction;

            return scenePosition;
        }

        /// <inheritdoc />
        public override void TriggerActiveRenderStageReevaluation()
        {
            var visgroups = SceneSystem.SceneInstance.VisibilityGroups;
            if (visgroups == null)
                return;

            foreach (var sceneInstanceVisibilityGroup in visgroups)
            {
                sceneInstanceVisibilityGroup.NeedActiveRenderStageReevaluation = true;
            }
        }

        public override void UpdateGraphicsCompositor(GraphicsCompositor graphicsCompositor)
        {
            base.UpdateGraphicsCompositor(graphicsCompositor);

            // We do not want cameras of the scene to attach to any camera slots, so let's remove all the slots.
            // Resolving properly the editor camera is done by EditorTopLevelCompositor anyway
            // Note: make sure to do that after base call so that services can access cameras
            graphicsCompositor.Cameras.Clear();
        }

        /// <summary>
        /// Initializes the <see cref="ContentScene"/>.
        /// </summary>
        /// <remarks>
        /// This method must be called only once.
        /// </remarks>
        /// <seealso cref="EnsureContentScene"/>
        internal void InitializeContentScene()
        {
            var contentScene = new Scene();
            ContentScene = contentScene;
            // Setup the scene for the game
            SceneSystem.SceneInstance.RootScene.Children.Add(contentScene);

            foreach (var service in EditorServices.Services)
            {
                service.RegisterScene(contentScene);
            }
        }

        protected Graphics.Effect ComputeMeshFallbackEffect(RenderObject renderObject, [NotNull] RenderEffect renderEffect, RenderEffectState renderEffectState)
        {
            try
            {
                var renderMesh = (RenderMesh)renderObject;

                bool hasDiffuseMap = renderMesh.MaterialPass.Parameters.ContainsKey(MaterialKeys.DiffuseMap);
                var fallbackMaterial = hasDiffuseMap
                    ? fallbackTextureMaterial
                    : fallbackColorMaterial;

                // High priority
                var compilerParameters = new CompilerParameters { EffectParameters = { TaskPriority = -1 } };

                // Support skinning
                if (renderMesh.Mesh.Skinning != null && renderMesh.Mesh.Skinning.Bones.Length <= 56)
                {
                    compilerParameters.Set(MaterialKeys.HasSkinningPosition, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningPosition));
                    compilerParameters.Set(MaterialKeys.HasSkinningNormal, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningNormal));
                    compilerParameters.Set(MaterialKeys.HasSkinningTangent, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningTangent));

                    compilerParameters.Set(MaterialKeys.SkinningMaxBones, 56);
                }

                // Set material permutations
                compilerParameters.Set(MaterialKeys.PixelStageSurfaceShaders, fallbackMaterial.Passes[0].Parameters.Get(MaterialKeys.PixelStageSurfaceShaders));
                compilerParameters.Set(MaterialKeys.PixelStageStreamInitializer, fallbackMaterial.Passes[0].Parameters.Get(MaterialKeys.PixelStageStreamInitializer));

                // Set lighting permutations (use custom white light, since this effect will not be processed by the lighting render feature)
                compilerParameters.Set(LightingKeys.EnvironmentLights, new ShaderSourceCollection { new ShaderClassSource("LightConstantWhite") });

                // Initialize parameters with material ones (need a CopyTo?)
                renderEffect.FallbackParameters = new ParameterCollection(renderMesh.MaterialPass.Parameters);

                // Don't show selection wireframe/highlights as compiling
                var ignoreState = renderEffect.EffectSelector.EffectName.EndsWith(".Wireframe") || renderEffect.EffectSelector.EffectName.EndsWith(".Highlight") ||
                                  renderEffect.EffectSelector.EffectName.EndsWith(".Picking");

                // Also set a value so that we know something is loading (green glowing FX) or error (red glowing FX)
                if (!ignoreState)
                {
                    if (renderEffectState == RenderEffectState.Compiling)
                        compilerParameters.Set(SceneEditorParameters.IsEffectCompiling, true);
                    else if (renderEffectState == RenderEffectState.Error)
                        compilerParameters.Set(SceneEditorParameters.IsEffectError, true);
                }

                if (renderEffectState == RenderEffectState.Error)
                {
                    // Retry every few seconds
                    renderEffect.RetryTime = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                }

                return EffectSystem.LoadEffect(renderEffect.EffectSelector.EffectName, compilerParameters).WaitForResult();
            }
            catch
            {
                // TODO: Log or rethrow?
                renderEffect.State = RenderEffectState.Error;
                return null;
            }
        }

        /// <summary>
        /// Ensures that the <see cref="ContentScene"/> has been initialized. Otherwise throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <seealso cref="InitializeContentScene"/>
        protected void EnsureContentScene()
        {
            if (ContentScene != null)
                return;

            throw new InvalidOperationException($"The {nameof(ContentScene)} has not been initialized yet.");
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            // Use a shared database for our shader system
            // TODO: Shaders compiled on main thread won't actually be visible to MicroThread build engine (contentIndexMap are separate).
            // It will still work and cache because EffectCompilerCache caches not only at the index map level, but also at the database level.
            // Later, we probably want to have a GetSharedDatabase() allowing us to mutate it (or merging our results back with IndexFileCommand.AddToSharedGroup()),
            // so that database created with MountDatabase also have all the newest shaders.
            ((IReferencable)effectCompiler).AddReference();
            EffectSystem.Compiler = effectCompiler;

            // Record used effects
            if (effectLogPath != null)
            {
                EffectSystem.EffectUsed += (request, result) =>
                {
                    if (result.HasErrors)
                        return;

                    using (var stream = File.Open(effectLogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        var requests = AssetYamlSerializer.Default.DeserializeMultiple<EffectCompileRequest>(stream).ToList();
                        if (requests.Contains(request))
                            return;

                        var documentMarker = Encoding.UTF8.GetBytes("---\r\n");
                        stream.Write(documentMarker, 0, documentMarker.Length);
                        AssetYamlSerializer.Default.Serialize(stream, request);
                    }
                };
            }

            //init physics system
            var physicsSystem = new Bullet2PhysicsSystem(Services);
            Services.AddService<IPhysicsSystem>(physicsSystem);
            GameSystems.Add(physicsSystem);
        }

        /// <inheritdoc />
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // mount the common database in this micro-thread to have access to the built effects, etc.
            MicrothreadLocalDatabases.MountCommonDatabase();

            // Create fallback effect to use when material is still loading
            fallbackColorMaterial = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            });

            fallbackTextureMaterial = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor { FallbackValue = null }), // Do not use fallback value, we want a DiffuseMap
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            });

            // Listen to all Renderer Initialized to plug dynamic effect compilation
            RenderContext.GetShared(Services).RendererInitialized += SceneGameRendererInitialized;
            // Update the marker render target setter viewport
            //OnClientSizeChanged(this, EventArgs.Empty);

            // Initialize the services
            var initialized = new List<IEditorGameService>();
            foreach (var service in EditorServices.OrderByDependency())
            {
                // Check that the current service dependencies have been initialized
                foreach (var dependency in service.Dependencies)
                {
                    if (!initialized.Any(x => dependency.IsInstanceOfType(x)))
                        throw new InvalidOperationException($"The service [{service.GetType().Name}] requires a service of type [{dependency.Name}] to be initialized first.");
                }
                if (await service.InitializeService(this))
                {
                    initialized.Add(service);
                }

                var mouseService = service as EditorGameMouseServiceBase;
                mouseService?.RegisterMouseServices(EditorServices);
            }

            // TODO: Maybe define this scene default graphics compositor as an asset?
            var defaultGraphicsCompositor = GraphicsCompositorHelper.CreateDefault(true, EditorGraphicsCompositorHelper.EditorForwardShadingEffect);

            // Add UI (engine doesn't depend on it)
            defaultGraphicsCompositor.RenderFeatures.Add(new UIRenderFeature
            {
                RenderStageSelectors =
                {
                    new SimpleGroupToRenderStageSelector
                    {
                        RenderStage = defaultGraphicsCompositor.RenderStages.First(x => x.Name == "Transparent"),
                        EffectName = "Test",
                        RenderGroup = GizmoBase.DefaultGroupMask
                    }
                }
            });

            // Make the game switch to this graphics compositor
            UpdateGraphicsCompositor(defaultGraphicsCompositor);

            gameContentLoadedTaskSource.SetResult(true);
        }

        private void CreateScenePipeline()
        {
            // The gizmo scene
            EditorScene = new Scene();

            // The gizmo  scene
            var gizmoAmbientLight1 = new Entity("Gizmo Ambient Light1") { new LightComponent { Type = new LightAmbient(), Intensity = 0.1f } };
            var gizmoDirectionalLight1 = new Entity("Gizmo Directional Light1") { new LightComponent { Type = new LightDirectional(), Intensity = 0.45f } };
            var gizmoDirectionalLight2 = new Entity("Gizmo Directional Light2") { new LightComponent { Type = new LightDirectional(), Intensity = 0.45f } };
            gizmoDirectionalLight1.Transform.Rotation = Quaternion.RotationY(MathUtil.Pi * 1.125f);
            gizmoDirectionalLight2.Transform.Rotation = Quaternion.RotationY(MathUtil.Pi * 0.125f);
            EditorScene.Entities.Add(gizmoAmbientLight1);
            EditorScene.Entities.Add(gizmoDirectionalLight1);
            EditorScene.Entities.Add(gizmoDirectionalLight2);

            // Root scene
            var rootScene = new Scene();

            // Setup the scene instance
            SceneSystem.SceneInstance = new SceneInstance(Services, rootScene, ExecutionMode.Editor);

            // Create editor scene
            EditorSceneSystem = new SceneSystem(Services)
            {
                SceneInstance = new SceneInstance(Services, EditorScene, ExecutionMode.Editor)
            };
            GameSystems.Add(EditorSceneSystem);

            EditorSceneSystem.DrawOrder = SceneSystem.DrawOrder + 1;
            EditorSceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(true, EditorGraphicsCompositorHelper.EditorForwardShadingEffect, groupMask: GizmoBase.DefaultGroupMask);

            // Do a few adjustments to the default compositor
            EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()?.RenderFeatures.Add(new WireframeRenderFeature());
            EditorSceneSystem.GraphicsCompositor.Game = new EditorTopLevelCompositor
            {
                Child = new ForwardRenderer
                {
                    Clear = null, // Don't clear (we want to keep reusing same render target and depth buffer)
                    OpaqueRenderStage = EditorSceneSystem.GraphicsCompositor.RenderStages.FirstOrDefault(x => x.Name == "Opaque"),
                    TransparentRenderStage = EditorSceneSystem.GraphicsCompositor.RenderStages.FirstOrDefault(x => x.Name == "Transparent"),
                },
            };
        }

        private void SceneGameRendererInitialized(IGraphicsRendererCore obj)
        {
            // TODO: callback will be called also for editor renderers. We might want to filter down this
            if (obj is MeshRenderFeature)
            {
                ((MeshRenderFeature)obj).ComputeFallbackEffect = ComputeMeshFallbackEffect;
            }
        }
    }
}
