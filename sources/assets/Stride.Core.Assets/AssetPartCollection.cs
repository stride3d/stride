// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;

namespace Stride.Core.Assets
{
    [DataSerializer(typeof(AssetPartCollectionSerializer<,>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class AssetPartCollection<TAssetPartDesign, TAssetPart> : SortedList<Guid, TAssetPartDesign>
        where TAssetPartDesign : IAssetPartDesign<TAssetPart>
        where TAssetPart : IIdentifiable
    {
        public void Add([NotNull] TAssetPartDesign part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            Add(part.Part.Id, part);
        }

        public void Add(KeyValuePair<Guid, TAssetPartDesign> part)
        {
            if (part.Value == null) throw new ArgumentNullException(nameof(part));
            if (part.Key != part.Value.Part.Id) throw new ArgumentException(@"The guid of the key does not match the guid of the value", nameof(part));
            Add(part.Key, part.Value);
        }

        /// <summary>
        /// Refreshes the keys of this collection. Must be called if some ids of the contained parts have changed.
        /// </summary>
        public void RefreshKeys()
        {
            var values = Values.ToList();
            Clear();
            foreach (var value in values)
            {
                Add(value.Part.Id, value);
            }
        }
    }

    public class AssetPartCollectionSerializer<TAssetPartDesign, TAssetPart> : DataSerializer<AssetPartCollection<TAssetPartDesign, TAssetPart>>, IDataSerializerGenericInstantiation
        where TAssetPartDesign : IAssetPartDesign<TAssetPart>
        where TAssetPart : IIdentifiable
    {
        private DataSerializer<TAssetPartDesign> valueSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            valueSerializer = MemberSerializer<TAssetPartDesign>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref AssetPartCollection<TAssetPartDesign, TAssetPart> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the SortedList size
                if (obj == null)
                    obj = new AssetPartCollection<TAssetPartDesign, TAssetPart>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref AssetPartCollection<TAssetPartDesign, TAssetPart> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                var count = stream.ReadInt32();
                for (var i = 0; i < count; ++i)
                {
                    var value = default(TAssetPartDesign);
                    valueSerializer.Serialize(ref value, mode, stream);
                    var key = value.Part.Id;
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        /// <inheritdoc/>
        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, [NotNull] IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(Guid));
            genericInstantiations.Add(typeof(TAssetPartDesign));
        }
    }
}
