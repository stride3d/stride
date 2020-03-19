// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.Engine
{
    /// <summary>
    /// Extensions for <see cref="Entity"/> and the <see cref="TransformComponent"/>.
    /// </summary>
    public static class EntityTransformExtensions
    {
        /// <summary>
        /// Adds a child Entity to the transform component of a parent Entity.
        /// </summary>
        /// <param name="parentEntity">The parent Entity.</param>
        /// <param name="childEntity">The child parent Entity.</param>
        /// <exception cref="NullReferenceException"><paramref name="childEntity"/> is <c>null</c></exception>
        /// <exception cref="NullReferenceException"><paramref name="parentEntity"/> is <c>null</c></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddChild([NotNull] this Entity parentEntity, [NotNull] Entity childEntity)
        {
            parentEntity.Transform.Children.Add(childEntity.Transform);
        }

        /// <summary>
        /// Removes a child Entity to the transform component of a parent Entity. Note that the child entity is still in the <see cref="SceneInstance"/>.
        /// In order to remove it from the scene instance, you should call <see cref="SceneInstance.Remove"/>
        /// </summary>
        /// <param name="parentEntity">The parent Entity.</param>
        /// <param name="childEntity">The child Entity.</param>
        /// <exception cref="NullReferenceException"><paramref name="childEntity"/> is <c>null</c></exception>
        /// <exception cref="NullReferenceException"><paramref name="parentEntity"/> is <c>null</c></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveChild([NotNull] this Entity parentEntity, [NotNull] Entity childEntity)
        {
            parentEntity.Transform.Children.Remove(childEntity.Transform);
        }

        /// <summary>
        /// Removes a child entity from the transform component of a parent Entity.
        /// </summary>
        /// <param name="parentEntity">The parent entity.</param>
        /// <param name="childId">The child id of the child entity.</param>
        /// <exception cref="NullReferenceException"><paramref name="parentEntity"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException"><paramref name="childId"/> is <see cref="Guid.Empty"/></exception>
        public static void RemoveChild([NotNull] this Entity parentEntity, Guid childId)
        {
            if (childId == Guid.Empty) throw new ArgumentException(nameof(childId));
            for (var i = 0; i < parentEntity.Transform.Children.Count; i++)
            {
                var child = parentEntity.Transform.Children[i];
                if (child.Entity.Id == childId)
                {
                    parentEntity.Transform.Children.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Get the nth child of an Entity as stored in its <see cref="TransformComponent"/>.
        /// </summary>
        /// <param name="parentEntity">The parent Entity.</param>
        /// <param name="index">The child index.</param>
        /// <exception cref="NullReferenceException"><paramref name="parentEntity"/> is <c>null</c></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetChild([NotNull] this Entity parentEntity, int index)
        {
            return parentEntity.Transform.Children[index].Entity;
        }

        /// <summary>
        /// Returns the parent of this <see cref="Entity"/> as stored in its <see cref="TransformComponent"/>, or null if it has no parent.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The parent entity, or null if it has no parent.</returns>
        /// <exception cref="NullReferenceException"><paramref name="entity"/> is <c>null</c></exception>
        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetParent([NotNull] this Entity entity)
        {
            return entity.Transform.Parent?.Entity;
        }

        /// <summary>
        /// Sets the parent of the <see cref="TransformComponent" /> of this <see cref="Entity" />. If the parent is set to <c>null</c>, the enity gets removed from the scene graph.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parent">The parent. Can be <c>null</c></param>
        /// 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetParent(this Entity entity, Entity parent)
        {
            entity.Transform.Parent = parent?.Transform;
        }

        /// <summary>
        /// Returns the first child in the hierarchy with the provided name.
        /// This function can be slow, do not use every frame!
        /// </summary>
        /// <param name="parentEntity">The parent Entity.</param>
        /// <param name="childName">The name of the child to look for.</param>
        /// <returns>Null or the first child with the requested name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parentEntity"/> is <c>null</c></exception>
        [CanBeNull]
        public static Entity FindChild([NotNull] this Entity parentEntity, string childName)
        {
            if (parentEntity == null) throw new ArgumentNullException(nameof(parentEntity));
            return Utilities.IterateTree(parentEntity, entity => entity?.GetChildren()).FirstOrDefault(entity => entity != null && entity.Name == childName);
        }

        /// <summary>
        /// Returns the root of this <see cref="Entity"/>, or itself if it has no parent.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The root entity, or itself if it has no parent.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <c>null</c></exception>
        [NotNull]
        public static Entity FindRoot([NotNull] this Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var root = entity;
            Entity parent;
            while ((parent = root.GetParent()) != null)
            {
                root = parent;
            }
            return root;
        }

        /// <summary>
        /// Gets absolute world space position, rotation and scale of the given transform.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="position">Output world space position.</param>
        /// <param name="rotation">Output world space rotation.</param>
        /// <param name="scale">Output world space scale.</param>
        public static void GetWorldTransformation(this TransformComponent transformComponent, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = Vector3.Zero;
            rotation = Quaternion.Identity;
            scale = Vector3.One;
            transformComponent.LocalToWorld(ref position, ref rotation, ref scale);
        }

        /// <summary>
        /// Performs tranformation of the given transform in world space to local space.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="position">Input world space position tranformed to local space.</param>
        /// <param name="rotation">Input world space rotation tranformed to local space.</param>
        /// <param name="scale">Input world space scale tranformed to local space.</param>
        public static void WorldToLocal(this TransformComponent transformComponent, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale)
        {
            Vector3 worldScale;
            Quaternion worldRotation;
            Vector3 worldTranslation;
            transformComponent.WorldMatrix.Decompose(out worldScale, out worldRotation, out worldTranslation);

            Matrix worldMatrixInv;
            Matrix.Invert(ref transformComponent.WorldMatrix, out worldMatrixInv);

            Vector3.Transform(ref position, ref worldMatrixInv, out position);
            worldRotation.Invert();
            Quaternion.Multiply(ref rotation, ref worldRotation, out rotation);
            Vector3.Demodulate(ref scale, ref worldScale, out scale);
        }

        /// <summary>
        /// Performs tranformation of the given point in world space to local space.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="point">World space point.</param>
        /// <param name="result">Local space point.</param>
        public static void WorldToLocal(this TransformComponent transformComponent, ref Vector3 point, out Vector3 result)
        {
            Matrix worldMatrixInv;
            Matrix.Invert(ref transformComponent.WorldMatrix, out worldMatrixInv);
            Vector3.Transform(ref point, ref worldMatrixInv, out result);
        }

        /// <summary>
        /// Performs tranformation of the given point in world space to local space.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="point">World space point.</param>
        /// <returns>Local space point.</returns>
        public static Vector3 WorldToLocal(this TransformComponent transformComponent, Vector3 point)
        {
            Vector3 result;
            Matrix worldMatrixInv;
            Matrix.Invert(ref transformComponent.WorldMatrix, out worldMatrixInv);
            Vector3.Transform(ref point, ref worldMatrixInv, out result);
            return result;
        }

        /// <summary>
        /// Performs tranformation of the given transform in local space to world space.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="position">Input local space position tranformed to world space.</param>
        /// <param name="rotation">Input local space rotation tranformed to world space.</param>
        /// <param name="scale">Input local space scale tranformed to world space.</param>
        public static void LocalToWorld(this TransformComponent transformComponent, ref Vector3 position, ref Quaternion rotation, ref Vector3 scale)
        {
            Vector3 worldScale;
            Quaternion worldRotation;
            Vector3 worldTranslation;
            transformComponent.WorldMatrix.Decompose(out worldScale, out worldRotation, out worldTranslation);

            Vector3.Transform(ref position, ref transformComponent.WorldMatrix, out position);
            Quaternion.Multiply(ref rotation, ref worldRotation, out rotation);
            Vector3.Modulate(ref scale, ref worldScale, out scale);
        }

        /// <summary>
        /// Performs tranformation of the given point in local space to world space.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="point">Local space point.</param>
        /// <param name="result">World space point.</param>
        public static void LocalToWorld(this TransformComponent transformComponent, ref Vector3 point, out Vector3 result)
        {
            Vector3.Transform(ref point, ref transformComponent.WorldMatrix, out result);
        }

        /// <summary>
        /// Performs tranformation of the given point in local space to world space.
        /// </summary>
        /// <param name="transformComponent">The transform component.</param>
        /// <param name="point">Local space point.</param>
        /// <returns>World space point.</returns>
        public static Vector3 LocalToWorld(this TransformComponent transformComponent, Vector3 point)
        {
            Vector3 result;
            Vector3.Transform(ref point, ref transformComponent.WorldMatrix, out result);
            return result;
        }
    }
}
