// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Serialization;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class AbstractNodeEntryNodeUpdater : AssetNodePresenterUpdaterBase
    {
        public static IEnumerable<AbstractNodeEntry> FillDefaultAbstractNodeEntry(IAssetNodePresenter node)
        {
            var type = node.Descriptor.GetInnerCollectionType();

            var abstractNodeMatchingEntries = AbstractNodeType.GetInheritedInstantiableTypes(type);
            IEnumerable<AbstractNodeEntry> abstractNodeMatchingEntries2 = [];
            foreach (var nodeType in abstractNodeMatchingEntries)
            {
                AbstractNodeEntry nodeEntry = IsEntityComponent(nodeType.Type) ? new AbstractNodeValue(null, nodeType.Type.Name, 0) : nodeType;
                abstractNodeMatchingEntries2 = abstractNodeMatchingEntries2.Append(nodeEntry);
            }

            if (abstractNodeMatchingEntries2 != null)
            {
                // Prepend the value that will allow to set the value to null, if this command is allowed.
                if (IsAllowingNull(node))
                    abstractNodeMatchingEntries2 = AbstractNodeValue.Null.Yield().Concat(abstractNodeMatchingEntries2);
            }
            return abstractNodeMatchingEntries2;

            static bool IsEntityComponent(Type type)
            {
                for (var t = type; t != null; t = t.BaseType)
                {
                    if (t.Name == "EntityComponent" && t.FullName == "Stride.Engine.EntityComponent")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if <see cref="MemberCollectionAttribute.NotNullItems"/> is present and set.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>True if null is a possible choice for this node, otherwise false.</returns>
        public static bool IsAllowingNull(IAssetNodePresenter node)
        {
            var abstractNodeAllowNull = true;
            var memberNode = node as MemberNodePresenter ?? (node as ItemNodePresenter)?.Parent as MemberNodePresenter;
            if (memberNode != null)
            {
                var memberCollection = memberNode.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                       ?? memberNode.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault();

                if (memberNode.IsEnumerable && memberCollection != null && memberCollection.NotNullItems)
                {
                    // Collections
                    abstractNodeAllowNull = false;
                }
                else
                {
                    // Members
                    abstractNodeAllowNull = !memberNode.MemberAttributes.OfType<NotNullAttribute>().Any();
                }
            }
            return abstractNodeAllowNull;
        }

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var type = node.Descriptor.GetInnerCollectionType();
            if (type.IsAbstract && IsInstantiable(type))
            {
                var abstractNodeEntries = FillDefaultAbstractNodeEntry(node);

                // Remove content types, the engine expects content types to be serialized as reference, not created inline
                if (AssetRegistry.CanPropertyHandleContent(type, out var contentTypes))
                    abstractNodeEntries = abstractNodeEntries.Where(x => x is AbstractNodeType ant == false || contentTypes.Contains(ant.Type) == false);

                node.AttachedProperties.Add(AbstractNodeEntryData.Key, abstractNodeEntries);
            }
        }

        private static bool IsInstantiable(Type type) => TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<NonInstantiableAttribute>(type) == null;
    }
}
