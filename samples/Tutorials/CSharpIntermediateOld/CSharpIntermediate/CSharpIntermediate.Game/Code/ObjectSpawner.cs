using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class ObjectSpawner : SyncScript
    {
        public Prefab ObjectToSpawn;

        private Entity prefabClone = null;

        public override void Start() { }

        public override void Update()
        {

            DebugText.Print("Press S to spawn ball", new Int2(300, 180));

            if (Input.IsKeyPressed(Keys.S))
            {
                prefabClone ??= ObjectToSpawn.Instantiate()[0];
                if (!Entity.Scene.Entities.Contains(prefabClone))
                {
                    Entity.Scene.Entities.Add(prefabClone);
                }

                Entity.Transform.GetWorldTransformation(out Vector3 worldPos, out Quaternion rot, out Vector3 scale);
                prefabClone.Transform.Position = worldPos;
                prefabClone.Transform.UpdateWorldMatrix();

                var physicsComponent = prefabClone.Get<RigidbodyComponent>();
                physicsComponent.Enabled = true;
                physicsComponent.LinearVelocity = new Vector3(0);
                physicsComponent.AngularVelocity = new Vector3(0);
                physicsComponent.UpdatePhysicsTransformation();
            }
        }

        public override void Cancel()
        {
            if (prefabClone != null)
            {
                Entity.Scene.Entities.Remove(prefabClone);
                prefabClone = null;
            }
        }
    }
}
