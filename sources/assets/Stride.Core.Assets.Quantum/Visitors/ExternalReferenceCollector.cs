// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum.Visitors
{
    /// <summary>
    /// A visitor that will collect all object references that target objects that are not included in the visited object.
    /// </summary>
    public class ExternalReferenceCollector : IdentifiableObjectVisitorBase
    {
        private readonly AssetPropertyGraphDefinition propertyGraphDefinition;

        private readonly HashSet<IIdentifiable> internalReferences = new HashSet<IIdentifiable>();
        private readonly HashSet<IIdentifiable> externalReferences = new HashSet<IIdentifiable>();
        private readonly Dictionary<IIdentifiable, List<NodeAccessor>> externalReferenceAccessors = new Dictionary<IIdentifiable, List<NodeAccessor>>();

        private ExternalReferenceCollector([NotNull] AssetPropertyGraphDefinition propertyGraphDefinition)
            : base(propertyGraphDefinition)
        {
            this.propertyGraphDefinition = propertyGraphDefinition;
        }

        /// <summary>
        /// Computes the external references to the given root node.
        /// </summary>
        /// <param name="propertyGraphDefinition">The property graph definition to use to analyze the graph.</param>
        /// <param name="root">The root node to analyze.</param>
        /// <returns>A set containing all external references to identifiable objects.</returns>
        [NotNull]
        public static HashSet<IIdentifiable> GetExternalReferences([NotNull] AssetPropertyGraphDefinition propertyGraphDefinition, [NotNull] IGraphNode root)
        {
            var visitor = new ExternalReferenceCollector(propertyGraphDefinition);
            visitor.Visit(root);
            // An IIdentifiable can have been recorded both as internal and external reference. In this case we still want to clone it so let's remove it from external references
            visitor.externalReferences.ExceptWith(visitor.internalReferences);
            return visitor.externalReferences;
        }

        /// <summary>
        /// Computes the external references to the given root node and their accessors.
        /// </summary>
        /// <param name="propertyGraphDefinition">The property graph definition to use to analyze the graph.</param>
        /// <param name="root">The root node to analyze.</param>
        /// <returns>A set containing all external references to identifiable objects.</returns>
        public static Dictionary<IIdentifiable, List<NodeAccessor>> GetExternalReferenceAccessors([NotNull] AssetPropertyGraphDefinition propertyGraphDefinition, [NotNull] IGraphNode root)
        {
            var visitor = new ExternalReferenceCollector(propertyGraphDefinition);
            visitor.Visit(root);
            // An IIdentifiable can have been recorded both as internal and external reference. In this case we still want to clone it so let's remove it from external references
            foreach (var internalReference in visitor.internalReferences)
            {
                visitor.externalReferenceAccessors.Remove(internalReference);
            }
            return visitor.externalReferenceAccessors;
        }

        protected override void ProcessIdentifiableMembers(IIdentifiable identifiable, IMemberNode member)
        {
            if (propertyGraphDefinition.IsMemberTargetObjectReference(member, identifiable))
            {
                externalReferences.Add(identifiable);
                if (!externalReferenceAccessors.TryGetValue(identifiable, out var accessors))
                {
                    externalReferenceAccessors.Add(identifiable, accessors = new List<NodeAccessor>());
                }
                accessors.Add(CurrentPath.GetAccessor());
            }
            else
                internalReferences.Add(identifiable);
        }

        protected override void ProcessIdentifiableItems(IIdentifiable identifiable, IObjectNode collection, NodeIndex index)
        {
            if (propertyGraphDefinition.IsTargetItemObjectReference(collection, index, identifiable))
            {
                externalReferences.Add(identifiable);
                if (!externalReferenceAccessors.TryGetValue(identifiable, out var accessors))
                {
                    externalReferenceAccessors.Add(identifiable, accessors = new List<NodeAccessor>());
                }
                accessors.Add(CurrentPath.GetAccessor());
            }
            else
                internalReferences.Add(identifiable);
        }
    }
}
