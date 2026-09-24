// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Concurrent;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Engine;
using Stride.Rendering;

namespace CSharpIntermediate.Code
{
    public class AsyncCollisionTriggerDemo : StartupScript, IContactHandler
    {
        private Material yellowMaterial;
        private StaticComponent staticCollider;
        private readonly ConcurrentDictionary<ModelComponent, Material> originalMaterials = new();

        bool IContactHandler.NoContactResponse => true;

        public override void Start()
        {
            // Store the collider component
            staticCollider = Entity.Get<StaticComponent>();
            staticCollider.ContactEventHandler = this;

            //Preload some materials
            yellowMaterial = Content.Load<Material>("Materials/Yellow");
        }

        void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
        {
            var modelComponent = contacts.Other.Entity.Get<ModelComponent>();
            if (modelComponent != null && originalMaterials.TryAdd(modelComponent, modelComponent.Materials[0]))
            {
                // Change the material on the entity
                modelComponent.Materials[0] = yellowMaterial;
            }
        }

        void IContactHandler.OnStoppedTouching<TManifold>(Contacts<TManifold> contacts)
        {
            var modelComponent = contacts.Other.Entity.Get<ModelComponent>();
            if (modelComponent != null && originalMaterials.TryRemove(modelComponent, out var originalMaterial))
            {
                // Change the material back to the original one
                modelComponent.Materials[0] = originalMaterial;
            }
        }

        public override void Cancel()
        {
            foreach (var (modelComponent, originalMaterial) in originalMaterials)
                modelComponent.Materials[0] = originalMaterial;

            originalMaterials.Clear();
            Content.Unload(yellowMaterial);
        }
    }
}
