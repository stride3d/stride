using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Yaml;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A custom serializer for asset part collections, that serializes this dictionary in the form of a collection.
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class AssetPartCollectionSerializer : CollectionSerializer, IDataCustomVisitor
    {
        private static readonly PropertyKey<object> OriginalValue = new PropertyKey<object>("OriginalValue", typeof(AssetPartCollectionSerializer));

        /// <inheritdoc/>
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.Type;
            return CanVisit(type) ? this : null;
        }

        /// <inheritdoc/>
        protected override bool CheckIsSequence(ref ObjectContext objectContext)
        {
            // We always want to serialize this collection as a sequence. Also, the base implementation relies on the fact that the descriptor
            // is a CollectionDescriptor which is not the case for this specific type.
            return true;
        }

        /// <inheritdoc/>
        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            // Build a list that matches the type of part design objects. Not that this relies on the fact that this type is the first generic argument of AssetPartCollection.
            var elementType = objectContext.Descriptor.Type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)objectContext.SerializerContext.ObjectFactory.Create(listType);

            if (objectContext.SerializerContext.IsSerializing)
            {
                var guidToIndex = new Dictionary<Guid, int>();
                // If we're writing, lets copy values of the dictionary into the list.
                var i = 0;
                foreach (DictionaryEntry entry in (IDictionary)objectContext.Instance)
                {
                    list.Add(entry.Value);
                    guidToIndex.Add((Guid)entry.Key, i++);
                }
                // We need to convert path in attached YAML metadata, from the guid keys of the dictionary to the indices of the list.
                FixupPaths(ref objectContext, guidToIndex);
            }
            else
            {
                // If we're reading, let's store the initial object to transfer items in TransformObjectAfterRead, in case there is no setter for the owning member.
                objectContext.Properties.Add(OriginalValue, objectContext.Instance);
            }
            // Apply the transformation to a list.
            objectContext.Instance = list;
        }

        /// <inheritdoc/>
        protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
            // Retrieve the list, and the original AssetPartCollection, and transfer items from one to the other.
            var list = (IList)objectContext.Instance;
            var partCollection = (IDictionary)objectContext.Properties.Get(OriginalValue);
            var indexToGuid = new Dictionary<int, Guid>();
            for (var i = 0; i < list.Count; ++i)
            {
                var partDesign = (IAssetPartDesign)list[i];
                partCollection.Add(partDesign.Part.Id, partDesign);
                indexToGuid.Add(i, partDesign.Part.Id);
            }

            // Restore the correct instance for the serializer.
            objectContext.Instance = partCollection;

            // We need to convert path in attached YAML metadata, from the integer indices of the list to the Guid that are keys of the dictionary.
            FixupPaths(ref objectContext, indexToGuid);
            base.TransformObjectAfterRead(ref objectContext);
        }

        /// <summary>
        /// Converts all <see cref="YamlAssetPath"/> from the metadata to switch between <see cref="Guid"/> keys of the
        /// <see cref="AssetPartCollection{TAssetPartDesign, TAssetPart}"/> and the integer indices of the serialized list, and vice-versa.
        /// </summary>
        /// <typeparam name="TIndexSource">The current type of indices in the metadata.</typeparam>
        /// <typeparam name="TIndexTarget">The type of indices to convert to.</typeparam>
        /// <param name="objectContext">The current object context of the serialization.</param>
        /// <param name="mapping">The mapping between the source indices and the target indices.</param>
        private static void FixupPaths<TIndexSource, TIndexTarget>(ref ObjectContext objectContext, Dictionary<TIndexSource, TIndexTarget> mapping)
        {
            var currentPath = AssetObjectSerializerBackend.GetCurrentPath(ref objectContext, false);
            foreach (var property in objectContext.SerializerContext.Properties)
            {
                if (typeof(IYamlAssetMetadata).IsAssignableFrom(property.Key.PropertyType))
                {
                    var metadata = (IYamlAssetMetadata)property.Value;
                    foreach (var entry in metadata.Cast<DictionaryEntry>().ToList())
                    {
                        var path = (YamlAssetPath)entry.Key;
                        // Only modify path that are "inside" the collection.
                        if (path.StartsWith(currentPath))
                        {
                            // Use the same beginning for the path.
                            var replacementPath = currentPath.Clone();
                            // Retrieve the index that was used (int or Guid).
                            var indexSource = (TIndexSource)path.Elements[currentPath.Elements.Count].Value;
                            // Fetch the corresponding target index (int or Guid).
                            var indexTarget = mapping[indexSource];
                            // Replace the initial index by the target index in our new path.
                            replacementPath.PushIndex(indexTarget);
                            // Finally push the rest of the original path, that shouldn't be different.
                            path.Elements.Skip(replacementPath.Elements.Count).ForEach(x => replacementPath.Push(x));
                            // And replace the entry in the dictionary of metadata
                            metadata.Remove(path);
                            metadata.Set(replacementPath, entry.Value);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public bool CanVisit(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetPartCollection<,>);
        }

        /// <inheritdoc/>
        public void Visit(ref VisitorContext context)
        {
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }
    }
}
