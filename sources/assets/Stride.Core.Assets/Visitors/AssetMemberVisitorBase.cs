// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Visitors
{
    public abstract class AssetMemberVisitorBase : AssetVisitorBase
    {
        /// <summary>
        /// Gets the <see cref="Core.Reflection.MemberPath"/> that will be checked against when visiting.
        /// </summary>
        /// <seealso cref="AssetVisitorBase.CurrentPath"/>
        protected MemberPath MemberPath { get; set; }

        /// <inheritdoc/>
        public override void VisitArrayItem(Array array, ArrayDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor)
        {
            if (CurrentPath.Match(MemberPath))
                VisitAssetMember(item, itemDescriptor);
            else
                base.VisitArrayItem(array, descriptor, index, item, itemDescriptor);
        }

        /// <inheritdoc/>
        public override void VisitCollectionItem(IEnumerable collection, CollectionDescriptor descriptor, int index, object item, ITypeDescriptor itemDescriptor)
        {
            if (CurrentPath.Match(MemberPath))
                VisitAssetMember(item, itemDescriptor);
            else
                base.VisitCollectionItem(collection, descriptor, index, item, itemDescriptor);
        }

        /// <inheritdoc/>
        public override void VisitDictionaryKeyValue(object dictionary, DictionaryDescriptor descriptor, object key, ITypeDescriptor keyDescriptor, object value, ITypeDescriptor valueDescriptor)
        {
            if (CurrentPath.Match(MemberPath))
            {
                var keyValueType = typeof(KeyValuePair<,>).MakeGenericType(keyDescriptor.Type, valueDescriptor.Type);
                var keyValueDescriptor = TypeDescriptorFactory.Find(keyValueType);
                var keyValuePair = Activator.CreateInstance(keyValueType, key, value);
                VisitAssetMember(keyValuePair, keyValueDescriptor);
            }
            else
            {
                base.VisitDictionaryKeyValue(dictionary, descriptor, key, keyDescriptor, value, valueDescriptor);
            }
        }

        /// <inheritdoc />
        public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
        {
            if (CurrentPath.Match(MemberPath))
                VisitAssetMember(obj, descriptor);
            else
                base.VisitObject(obj, descriptor, visitMembers);
        }

        /// <inheritdoc/>
        public override void VisitObjectMember(object container, ObjectDescriptor containerDescriptor, IMemberDescriptor member, object value)
        {
            if (CurrentPath.Match(MemberPath))
                VisitAssetMember(value, member.TypeDescriptor);
            else
                base.VisitObjectMember(container, containerDescriptor, member, value);
        }

        /// <inheritdoc/>
        public override void VisitPrimitive(object primitive, PrimitiveDescriptor descriptor)
        {
            if (CurrentPath.Match(MemberPath))
                VisitAssetMember(primitive, descriptor);
            else
                base.VisitPrimitive(primitive, descriptor);
        }

        /// <summary>
        /// Called when <see cref="AssetVisitorBase.CurrentPath"/> matches the <see cref="MemberPath"/> given when creating this instance.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="descriptor"></param>
        protected abstract void VisitAssetMember(object value, ITypeDescriptor descriptor);
    }
}
