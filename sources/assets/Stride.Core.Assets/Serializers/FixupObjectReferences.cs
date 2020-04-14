// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// A static class that can be used to fix up object references.
    /// </summary>
    public static class FixupObjectReferences
    {
        /// <summary>
        /// Fix up references represented by the <paramref name="objectReferences"/> dictionary into the <paramref name="root"/> object, by visiting the object
        /// to find all <see cref="IIdentifiable"/> instances it references, and modify the references described by <paramref name="objectReferences"/> to point
        /// to the proper identifiable object matching the same <see cref="Guid"/>.
        /// </summary>
        /// <param name="root">The root object to fix up.</param>
        /// <param name="objectReferences">The path to each object reference and the <see cref="Guid"/> of the tar</param>
        /// <param name="clearBrokenObjectReferences">If true, any object refernce that cannot be resolved will be reset to null.</param>
        /// <param name="throwOnDuplicateIds">If true, an exception will be thrown if two <see cref="IIdentifiable"/></param>
        /// <param name="logger">An optional logger.</param>
        public static void RunFixupPass(object root, YamlAssetMetadata<Guid> objectReferences, bool clearBrokenObjectReferences, bool throwOnDuplicateIds, [CanBeNull] ILogger logger = null)
        {
            // First collect IIdentifiable objects
            var referenceTargets = CollectReferenceableObjects(root, objectReferences, throwOnDuplicateIds, logger);

            // Then resolve and update object references
            FixupReferences(root, objectReferences, referenceTargets, clearBrokenObjectReferences, logger);
        }

        public static Dictionary<Guid, IIdentifiable> CollectReferenceableObjects(object root, YamlAssetMetadata<Guid> objectReferences, bool throwOnDuplicateIds, [CanBeNull] ILogger logger = null)
        {
            var hashSet = new HashSet<MemberPath>(objectReferences.Select(x => x.Key.ToMemberPath(root)));
            var visitor = new FixupObjectReferenceVisitor(hashSet, throwOnDuplicateIds, logger);
            visitor.Visit(root);
            return visitor.ReferenceableObjects;
        }

        public static void FixupReferences([NotNull] object root, [NotNull] YamlAssetMetadata<Guid> objectReferences, [NotNull] Dictionary<Guid, IIdentifiable> referenceTargets, bool clearMissingReferences, ILogger logger = null)
        {
            FixupReferences(root, objectReferences, referenceTargets, clearMissingReferences, (p, r, v) => p.Apply(r, MemberPathAction.ValueSet, v));
        }

        public static void FixupReferences([NotNull] object root, [NotNull] YamlAssetMetadata<Guid> objectReferences, [NotNull] Dictionary<Guid, IIdentifiable> referenceTargets, bool clearMissingReferences, [NotNull] Action<MemberPath, object, object> applyAction, ILogger logger = null)
        {
            foreach (var objectReference in objectReferences)
            {
                var path = objectReference.Key.ToMemberPath(root);
                if (!referenceTargets.TryGetValue(objectReference.Value, out IIdentifiable target))
                {
                    logger?.Warning($"Unable to resolve target object [{objectReference.Value}] of reference [{objectReference.Key}]");
                    if (clearMissingReferences)
                        applyAction(path, root, null);
                }
                else
                {
                    var current = path.GetValue(root);
                    if (!Equals(current, target))
                    {
                        applyAction(path, root, target);
                    }
                }
            }
        }

        private class FixupObjectReferenceVisitor : DataVisitorBase
        {
            public readonly Dictionary<Guid, IIdentifiable> ReferenceableObjects = new Dictionary<Guid, IIdentifiable>();
            private readonly HashSet<MemberPath> objectReferences;
            private readonly bool throwOnDuplicateIds;
            private readonly ILogger logger;

            public FixupObjectReferenceVisitor(HashSet<MemberPath> objectReferences, bool throwOnDuplicateIds, [CanBeNull] ILogger logger = null)
            {
                this.objectReferences = objectReferences;
                this.throwOnDuplicateIds = throwOnDuplicateIds;
                this.logger = logger;
            }

            public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
            {
                var identifiable = obj as IIdentifiable;
                if (obj is IIdentifiable)
                {
                    // Skip reference, we're looking for real objects
                    if (!objectReferences.Any(x => x.Match(CurrentPath)))
                    {
                        if (ReferenceableObjects.ContainsKey(identifiable.Id))
                        {
                            var message = $"Multiple identifiable objects with the same id {identifiable.Id}";
                            logger?.Error(message);
                            if (throwOnDuplicateIds)
                                throw new InvalidOperationException(message);
                        }
                        ReferenceableObjects[identifiable.Id] = identifiable;
                    }
                }
                base.VisitObject(obj, descriptor, visitMembers);
            }
        }
    }
}
