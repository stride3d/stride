// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Stride.Assets.Presentation.AssetEditors.PrefabEditor.Game
{
    public class PrefabEditorLightService : EditorGameServiceBase
    {
        public override void RegisterScene(Scene scene)
        {
            base.RegisterScene(scene);

            CreateLight("Prefab Editor Ambient Light", new LightAmbient(), 0.3f, scene);

            var directionalLight1 = CreateLight("Prefab Editor Ambient Light1", new LightDirectional(), 2.5f, scene);
            directionalLight1.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.Pi * 1.125f, MathUtil.Pi * -0.125f, 0.0f);

            var directionalLight2 = CreateLight("Prefab Editor Ambient Light2", new LightDirectional(), 2.5f, scene);
            directionalLight2.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.Pi * 0.125f, MathUtil.Pi * -0.25f, 0.0f);
        }

        /// <inheritdoc />
        protected override Task<bool> Initialize(EditorServiceGame game)
        {
            return Task.FromResult(true);
        }

        [NotNull]
        private static Entity CreateLight(string name, ILight light, float intensity, [NotNull] Scene scene)
        {
            var entity = new Entity(name) { new LightComponent { Type = light, Intensity = intensity } };
            entity.Tags.Add(GizmoBase.NoGizmoKey, true);
            scene.Entities.Add(entity);
            return entity;
        }
    }
}
