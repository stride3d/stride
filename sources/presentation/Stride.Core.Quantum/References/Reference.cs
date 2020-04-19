// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Threading;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum.References
{
    internal static class Reference
    {
        private static readonly ThreadLocal<int> CreatingReference = new ThreadLocal<int>();

        [Obsolete("This method will be rewritten to handle separately the different types of references")]
        internal static IReference CreateReference(object objectValue, Type objectType, NodeIndex index, bool isMember)
        {
            if (objectValue != null && !objectType.IsInstanceOfType(objectValue)) throw new ArgumentException(@"objectValue type does not match objectType", nameof(objectValue));

            if (!CreatingReference.IsValueCreated)
                CreatingReference.Value = 0;

            ++CreatingReference.Value;

            IReference reference;
            var isCollection = HasCollectionReference(objectValue?.GetType() ?? objectType);
            if (isMember)
            {
                reference = new ObjectReference(objectValue, objectType, index);
            }
            else
            {
                if (objectValue != null && isCollection && index.IsEmpty)
                {
                    reference = new ReferenceEnumerable((IEnumerable)objectValue, objectType);
                }
                else
                {
                    reference = null;
                }
            }

            --CreatingReference.Value;

            return reference;
        }

        private static bool HasCollectionReference(Type type)
        {
            return type.IsArray || CollectionDescriptor.IsCollection(type) || DictionaryDescriptor.IsDictionary(type);
        }

        internal static void CheckReferenceCreationSafeGuard()
        {
            if (!CreatingReference.IsValueCreated || CreatingReference.Value == 0)
                throw new InvalidOperationException("A reference can only be constructed with the method Reference.CreateReference");
        }
    }
}
