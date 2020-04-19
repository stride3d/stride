// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Extensions
{
    public static class NodePresenterExtensions
    {
        /// <summary>
        /// Creates a virtual node in the given node to represents a cateogry, and move the children with the given names into this category.
        /// </summary>
        /// <param name="node">The node in which to create a category.</param>
        /// <param name="categoryName">The display name of the category.</param>
        /// <param name="order">The order of the category, relative to other children orders.</param>
        /// <param name="expand">The expand rule of the category.</param>
        /// <param name="propertiesToMove">The child property of the given node that should be moved into this category.</param>
        /// <returns>A new <see cref="AssetVirtualNodePresenter"/> that represents the created category.</returns>
        /// <remarks>This method will add an entry <c>Category</c> with a value of <c>true</c> in the associated data of the category.</remarks>
        /// <exception cref="InvalidOperationException">The category already exists.</exception>
        [NotNull]
        public static AssetVirtualNodePresenter CreateCategory([NotNull] this INodePresenter node, string categoryName, int? order, ExpandRule? expand, [CanBeNull] params string[] propertiesToMove)
        {
            var categoryPropertyName = CategoryData.ComputeCategoryNodeName(categoryName);
            if (node.Children.Any(x => x.Name == categoryPropertyName))
                throw new InvalidOperationException("The category has already been created or a node with a conflicting name exists.");

            var factory = (AssetNodePresenterFactory)node.Factory;
            var newNode = factory.CreateVirtualNodePresenter(node, categoryPropertyName, typeof(string), order, () => categoryName);
            newNode.AttachedProperties.Add(CategoryData.Key, true);
            newNode.AttachedProperties.Add(DisplayData.AutoExpandRuleKey, expand ?? ExpandRule.Always);
            newNode.DisplayName = categoryName;
            if (propertiesToMove != null)
            {
                foreach (var propertyToMove in propertiesToMove)
                {
                    node[propertyToMove].ChangeParent(newNode);
                }
            }
            return newNode;
        }

        /// <summary>
        /// Gets the node that corresponds to the given category name, if it exists. Otherwise, returns <c>null</c>.
        /// </summary>
        /// <param name="node">The node that contains the category.</param>
        /// <param name="categoryName">The name of the category.</param>
        /// <returns>The node that corresponds to the given category name, or <c>null</c>.</returns>
        public static AssetVirtualNodePresenter GetCategory([NotNull] this INodePresenter node, string categoryName)
        {
            var categoryPropertyName = CategoryData.ComputeCategoryNodeName(categoryName);
            var category = node.Children.FirstOrDefault(x => x.Name == categoryPropertyName);
            return (AssetVirtualNodePresenter)category;
        }

        /// <summary>
        /// Indicates whether the given node has a category node with the given name.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="categoryName">The name of the category.</param>
        /// <returns><c>true</c> if the node has a category with the given name, <c>false</c> otherwise.</returns>
        public static bool HasCategory([NotNull] this INodePresenter node, string categoryName)
        {
            return node.GetCategory(categoryName) != null;
        }

        /// <summary>
        /// Hides the node and merges its children into its parent's children.
        /// </summary>
        /// <param name="node">The node to bypass.</param>
        /// <exception cref="InvalidOperationException">The node is a root node or has no parent.</exception>
        public static void BypassNode([NotNull] this INodePresenter node)
        {
            if (node.Parent == null)
                throw new InvalidOperationException("The root node cannot be bypassed.");

            node.IsVisible = false;
            foreach (var child in node.Children.ToList())
            {
                child.ChangeParent(node.Parent);
            }
        }
    }
}
