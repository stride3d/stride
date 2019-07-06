// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine.Design;

namespace Xenko.Physics
{
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializer<PhysicsColliderShape>))]
    [DataSerializerGlobal(typeof(CloneSerializer<PhysicsColliderShape>), Profile = "Clone")]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<PhysicsColliderShape>), Profile = "Content")]
    public class PhysicsColliderShape : IDisposable
    {
        public PhysicsColliderShape()
        {
        }

        public PhysicsColliderShape([NotNull] IEnumerable<IAssetColliderShapeDesc> descriptions)
        {
            Descriptions.AddRange(descriptions);
        }

        /// <summary>
        /// Used to serialize one or more collider shapes into one single shape
        /// Reading this value will automatically parse the Shape property into its description
        /// Writing this value will automatically compose, create and populate the Shape property
        /// </summary>
        [DataMember]
        public List<IAssetColliderShapeDesc> Descriptions { get; } = new List<IAssetColliderShapeDesc>();

        [DataMemberIgnore]
        public ColliderShape Shape { get; internal set; }

        [NotNull]
        public static PhysicsColliderShape New([NotNull] params IAssetColliderShapeDesc[] descriptions)
        {
            if (descriptions == null) throw new ArgumentNullException(nameof(descriptions));
            return new PhysicsColliderShape(descriptions);
        }

        internal static ColliderShape Compose(IReadOnlyList<IAssetColliderShapeDesc> descs)
        {
            if (descs == null)
            {
                return null;
            }

            ColliderShape res = null;

            if (descs.Count == 1) //single shape case
            {
                res = CreateShape(descs[0]);
                if (res == null) return null;
                res.IsPartOfAsset = true;
            }
            else if (descs.Count > 1) //need a compound shape in this case
            {
                var compound = new CompoundColliderShape();
                foreach (var desc in descs)
                {
                    var subShape = CreateShape(desc);
                    if (subShape == null) continue;
                    compound.AddChildShape(subShape);
                }
                res = compound;
                res.IsPartOfAsset = true;
            }

            return res;
        }

        internal static ColliderShape CreateShape(IColliderShapeDesc desc)
        {
            if (desc == null)
                return null;

            ColliderShape shape = desc.CreateShape();
            
            if (shape == null) return null;

            //shape.UpdateLocalTransformations();
            shape.Description = desc;

            return shape;
        }

        public void Dispose()
        {
            if (Shape == null) return;

            var compound = Shape.Parent;
            compound?.RemoveChildShape(Shape);

            Shape.Dispose();
            Shape = null;
        }
    }
}
