// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

using Stride.Core.Assets.Visitors;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Serialization;

namespace Stride.Core.Assets.Analysis
{
    /// <summary>
    /// This analysis provides a method for visiting asset and file references 
    /// (<see cref="Core.Serialization.Contents.IReference" /> or <see cref="UFile" /> or <see cref="UDirectory" />)
    /// </summary>
    public static class AssetReferenceAnalysis
    {
        private static readonly object CachingLock = new object();

        private static readonly Dictionary<object, List<AssetReferenceLink>> CachingReferences = new Dictionary<object, List<AssetReferenceLink>>();

        private static bool enableCaching;

        /// <summary>
        /// Gets or sets the enable caching. Only used when loading packages
        /// </summary>
        /// <value>The enable caching.</value>
        internal static bool EnableCaching
        {
            get
            {
                return enableCaching;
            }
            set
            {
                lock (CachingLock)
                {
                    if (enableCaching != value)
                    {
                        CachingReferences.Clear();
                    }

                    enableCaching = value;
                }
            }
        }

        /// <summary>
        /// Gets all references (subclass of <see cref="Core.Serialization.Contents.IReference" /> and <see cref="UFile" />) from the specified asset
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>A list of references.</returns>
        public static List<AssetReferenceLink> Visit(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            List<AssetReferenceLink> assetReferences = null;

            lock (CachingLock)
            {
                if (enableCaching)
                {
                    if (CachingReferences.TryGetValue(obj, out assetReferences))
                    {
                        assetReferences = new List<AssetReferenceLink>(assetReferences);
                    }
                }
            }

            if (assetReferences == null)
            {
                assetReferences = new List<AssetReferenceLink>();
                
                var assetReferenceVistor = new AssetReferenceVistor { References = assetReferences };
                assetReferenceVistor.Visit(obj);

                lock (CachingLock)
                {
                    if (enableCaching)
                    {
                        CachingReferences[obj] = assetReferences;
                    }
                }
            }

            return assetReferences;
        }

        private class AssetReferenceVistor : AssetVisitorBase
        {
            public AssetReferenceVistor()
            {
                References = new List<AssetReferenceLink>();
            }

            public List<AssetReferenceLink> References { get; set; }

            public override void VisitArrayItem(Array array, ArrayDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor)
            {
                base.VisitArrayItem(array, descriptor, index, item, itemDescriptor);
                var assetReference = item as AssetReference;
                var attachedReference = AttachedReferenceManager.GetAttachedReference(item);
                if (assetReference != null)
                {
                    AddLink(item,
                        (guid, location) =>
                        {
                            var newValue = AssetReference.New(guid ?? assetReference.Id, location);
                            array.SetValue(newValue, index);
                            return newValue;
                        });
                }
                else if (attachedReference != null)
                {
                    AddLink(attachedReference,
                        (guid, location) =>
                        {
                            object newValue = guid.HasValue && guid.Value != AssetId.Empty ? AttachedReferenceManager.CreateProxyObject(descriptor.ElementType, guid.Value, location) : null;
                            array.SetValue(newValue, index);
                            return newValue;
                        });
                }
                else if (item is UFile)
                {
                    AddLink(item,
                        (guid, location) =>
                        {
                            var newValue = new UFile(location);
                            array.SetValue(newValue, index);
                            return newValue;
                        });
                }
                else if (item is UDirectory)
                {
                    AddLink(item,
                        (guid, location) =>
                        {
                            var newValue = new UFile(location);
                            array.SetValue(newValue, index);
                            return newValue;
                        });
                }
            }

            public override void VisitCollectionItem(IEnumerable collection, CollectionDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor)
            {
                base.VisitCollectionItem(collection, descriptor, index, item, itemDescriptor);
                var assetReference = item as AssetReference;
                var attachedReference = AttachedReferenceManager.GetAttachedReference(item);

                // We cannot set links if we do not have indexer accessor
                if (!descriptor.HasIndexerAccessors)
                    return;

                if (assetReference != null)
                {
                    AddLink(assetReference, (guid, location) =>
                    {
                        var link = AssetReference.New(guid ?? assetReference.Id, location);
                        descriptor.SetValue(collection, index, link);
                        return link;
                    });
                }
                else if (attachedReference != null)
                {
                    AddLink(attachedReference, (guid, location) =>
                    {
                        var link = guid.HasValue && guid.Value != AssetId.Empty ? AttachedReferenceManager.CreateProxyObject(descriptor.ElementType, guid.Value, location) : null;
                        descriptor.SetValue(collection, index, link);
                        return link;
                    });
                }
                else if (item is UFile)
                {
                    AddLink(item, (guid, location) =>
                    {
                        var link = new UFile(location);
                        descriptor.SetValue(collection, index, link);
                        return link;
                    });
                }
                else if (item is UDirectory)
                {
                    AddLink(item, (guid, location) =>
                    {
                        var link = new UDirectory(location);
                        descriptor.SetValue(collection, index, link);
                        return link;
                    });
                }
            }

            public override void VisitDictionaryKeyValue(object dictionaryObj, DictionaryDescriptor descriptor, object key, ITypeDescriptor keyDescriptor, object value, ITypeDescriptor valueDescriptor)
            {
                base.VisitDictionaryKeyValue(dictionaryObj, descriptor, key, keyDescriptor, value, valueDescriptor);
                var assetReference = value as AssetReference;
                var attachedReference = AttachedReferenceManager.GetAttachedReference(value);
                if (assetReference != null)
                {
                    AddLink(assetReference,
                        (guid, location) =>
                        {
                            var newValue = AssetReference.New(guid ?? assetReference.Id, location);
                            descriptor.SetValue(dictionaryObj, key, newValue);
                            return newValue;
                        });
                }
                else if (attachedReference != null)
                {
                    AddLink(attachedReference,
                        (guid, location) =>
                        {
                            object newValue = guid.HasValue && guid.Value != AssetId.Empty ? AttachedReferenceManager.CreateProxyObject(descriptor.ValueType, guid.Value, location) : null;
                            descriptor.SetValue(dictionaryObj, key, newValue);
                            return newValue;
                        });
                }
                else if (value is UFile)
                {
                    AddLink(value,
                        (guid, location) =>
                        {
                            var newValue = new UFile(location);
                            descriptor.SetValue(dictionaryObj, key, newValue);
                            return newValue;
                        });
                }
                else if (value is UDirectory)
                {
                    AddLink(value,
                        (guid, location) =>
                        {
                            var newValue = new UDirectory(location);
                            descriptor.SetValue(dictionaryObj, key, newValue);
                            return newValue;
                        });
                }
            }

            public override void VisitObjectMember(object container, ObjectDescriptor containerDescriptor, IMemberDescriptor member, object value)
            {
                base.VisitObjectMember(container, containerDescriptor, member, value);
                var assetReference = value as AssetReference;
                var attachedReference = AttachedReferenceManager.GetAttachedReference(value);
                if (assetReference != null)
                {
                    AddLink(assetReference,
                        (guid, location) =>
                        {
                            var newValue = AssetReference.New(guid ?? assetReference.Id, location);
                            member.Set(container, newValue);
                            return newValue;
                        });
                }
                else if (attachedReference != null)
                {
                    AddLink(attachedReference,
                        (guid, location) =>
                        {
                            object newValue = guid.HasValue && guid.Value != AssetId.Empty ? AttachedReferenceManager.CreateProxyObject(member.Type, guid.Value, location) : null;
                            member.Set(container, newValue);
                            return newValue;
                        });
                }
                else if (value is UFile)
                {
                    AddLink(value,
                        (guid, location) =>
                        {
                            var newValue = new UFile(location);
                            member.Set(container, newValue);
                            return newValue;
                        });
                }
                else if (value is UDirectory)
                {
                    AddLink(value,
                        (guid, location) =>
                        {
                            var newValue = new UDirectory(location);
                            member.Set(container, newValue);
                            return newValue;
                        });
                }
            }

            private void AddLink(object value, Func<AssetId?, string, object> updateReference)
            {
                References.Add(new AssetReferenceLink(CurrentPath.Clone(), value, updateReference));
            }
        }
    }
}
