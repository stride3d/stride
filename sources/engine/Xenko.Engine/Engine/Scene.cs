// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Engine
{
    /// <summary>
    /// A scene.
    /// </summary>
    [DataContract("Scene")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<Scene>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Scene>), Profile = "Content")]
    public sealed class Scene : ComponentBase, IIdentifiable
    {
        private Scene parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        public Scene()
        {
            Id = Guid.NewGuid();
            Entities = new TrackingCollection<Entity>();
            Entities.CollectionChanged += Entities_CollectionChanged;

            Children = new TrackingCollection<Scene>();
            Children.CollectionChanged += Children_CollectionChanged;
        }

        [DataMember(-10)]
        [Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        /// <summary>
        /// The parent scene.
        /// </summary>
        [DataMemberIgnore]
        public Scene Parent
        {
            get { return parent; }
            set
            {
                var oldParent = Parent;
                if (oldParent == value)
                    return;

                oldParent?.Children.Remove(this);
                value?.Children.Add(this);
            }
        }

        /// <summary>
        /// The entities.
        /// </summary>
        public TrackingCollection<Entity> Entities { get; }

        /// <summary>
        /// The child scenes.
        /// </summary>
        [DataMemberIgnore]
        public TrackingCollection<Scene> Children { get; }

        /// <summary>
        /// An offset applied to all entities of the scene relative to it's parent scene.
        /// </summary>
        public Vector3 Offset;

        /// <summary>
        /// The absolute transform applied to all entities of the scene.
        /// </summary>
        /// <remarks>This field is overwritten by the transform processor each frame.</remarks>
        public Matrix WorldMatrix;

        /// <summary>
        /// Updates the world transform of the scene.
        /// </summary>
        public void UpdateWorldMatrix()
        {
            UpdateWorldMatrixInternal(true);
        }

        internal void UpdateWorldMatrixInternal(bool isRecursive)
        {
            if (parent != null)
            {
                if (isRecursive)
                {
                    parent.UpdateWorldMatrixInternal(true);
                }

                WorldMatrix = parent.WorldMatrix;
            }
            else
            {
                WorldMatrix = Matrix.Identity;
            }

            WorldMatrix.TranslationVector += Offset;
        }

        public override string ToString()
        {
            return $"Scene {Name}";
        }

        private void Children_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddItem((Scene)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveItem((Scene)e.Item);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void AddItem(Scene item)
        {
            if (item.Parent != null)
                throw new InvalidOperationException("This scene already has a Parent. Detach it first.");

            item.parent = this;
        }

        private void RemoveItem(Scene item)
        {
            if (item.Parent != this)
                throw new InvalidOperationException("This scene's parent is not the expected value.");

            item.parent = null;
        }

        private void Entities_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddItem((Entity)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveItem((Entity)e.Item);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void AddItem(Entity item)
        {
            // Root entity in another scene, or child of another entity
            if (item.Scene != null)
                throw new InvalidOperationException("This entity already has a scene. Detach it first.");

            item.SceneValue = this;
        }

        private void RemoveItem(Entity item)
        {
            if (item.SceneValue != this)
                throw new InvalidOperationException("This entity's scene is not the expected value.");

            item.SceneValue = null;
        }
    }
}
