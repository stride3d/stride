// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Assets
{
    [DataContract]
    [DataSerializer(typeof(BasePartDataSerializer))]
    public sealed class BasePart : IEquatable<BasePart>
    {
        /// <inheritdoc />
        public bool Equals(BasePart other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(BasePartAsset, other.BasePartAsset) && BasePartId.Equals(other.BasePartId) && InstanceId.Equals(other.InstanceId);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as BasePart);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode - this property is not supposed to be changed except by asset analysis
                var hashCode = BasePartAsset != null ? BasePartAsset.GetHashCode() : 0;
                // ReSharper restore NonReadonlyMemberInGetHashCode
                hashCode = (hashCode * 397) ^ BasePartId.GetHashCode();
                hashCode = (hashCode * 397) ^ InstanceId.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc />
        public static bool operator ==(BasePart left, BasePart right)
        {
            return Equals(left, right);
        }

        /// <inheritdoc />
        public static bool operator !=(BasePart left, BasePart right)
        {
            return !Equals(left, right);
        }

        public BasePart([NotNull] AssetReference basePartAsset, Guid basePartId, Guid instanceId)
        {
            if (basePartAsset == null) throw new ArgumentNullException(nameof(basePartAsset));
            if (basePartId == Guid.Empty) throw new ArgumentException(nameof(basePartAsset));
            if (instanceId == Guid.Empty) throw new ArgumentException(nameof(basePartAsset));
            BasePartAsset = basePartAsset;
            BasePartId = basePartId;
            InstanceId = instanceId;
        }

        [DataMember(10)]
        // This property might be updated by asset analysis, we want to keep a public setter for that reason. But it shouldn't be used in normal cases! (until we get a better solution)
        public AssetReference BasePartAsset { get; set; }

        [DataMember(20)]
        public Guid BasePartId { get; }

        [DataMember(30)]
        public Guid InstanceId { get; }

        [CanBeNull]
        public IIdentifiable ResolvePart(PackageSession session)
        {
            var assetItem = session.FindAsset(BasePartAsset.Id);
            var asset = assetItem?.Asset as AssetComposite;
            return asset?.FindPart(BasePartId);
        }
    }

    public class BasePartDataSerializer : DataSerializer<BasePart>
    {
        /// <inheritdoc/>
        public override void Serialize(ref BasePart basePart, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(basePart.BasePartAsset);
                stream.Write(basePart.BasePartId);
                stream.Write(basePart.InstanceId);
            }
            else
            {
                var asset = stream.Read<AssetReference>();
                var baseId = stream.Read<Guid>();
                var instanceId = stream.Read<Guid>();
                basePart = new BasePart(asset, baseId, instanceId);
            }
        }
    }

    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class BasePartYamlSerializer : ObjectSerializer, IDataCustomVisitor
    {
        public override IYamlSerializable TryCreate(Core.Yaml.Serialization.SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return CanVisit(typeDescriptor.Type) ? this : null;
        }

        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            objectContext.Instance = objectContext.SerializerContext.IsSerializing ? new BasePartMutable((BasePart)objectContext.Instance) : new BasePartMutable();
        }

        protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
            objectContext.Instance = ((BasePartMutable)objectContext.Instance).ToBasePart();
        }

        private class BasePartMutable
        {
            public BasePartMutable()
            {
            }

            public BasePartMutable(BasePart item)
            {
                BasePartAsset = item.BasePartAsset;
                BasePartId = item.BasePartId;
                InstanceId = item.InstanceId;
            }

            // ReSharper disable MemberCanBePrivate.Local
            // ReSharper disable FieldCanBeMadeReadOnly.Local
            [DataMember(10)]
            public AssetReference BasePartAsset;

            [DataMember(20)]
            public Guid BasePartId;

            [DataMember(30)]
            public Guid InstanceId;
            // ReSharper restore FieldCanBeMadeReadOnly.Local
            // ReSharper restore MemberCanBePrivate.Local

            public BasePart ToBasePart()
            {
                return new BasePart(BasePartAsset, BasePartId, InstanceId);
            }
        }

        public bool CanVisit(Type type)
        {
            return type == typeof(BasePart);
        }

        public void Visit(ref VisitorContext context)
        {
            context.Visitor.VisitObject(context.Instance, context.Descriptor, true);
        }
    }

}
