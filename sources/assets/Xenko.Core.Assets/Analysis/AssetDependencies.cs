// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Assets.Analysis
{
    /// <summary>
    /// Describes dependencies (in/out/broken) for a specific asset.
    /// </summary>
    /// <remarks>There are 3 types of dependencies:
    /// <ul>
    /// <li><c>in</c> dependencies: through the <see cref="LinksIn"/> property, contains assets                                 
    /// that are referencing this asset.</li>
    /// <li><c>out</c> dependencies: through the <see cref="LinksOut"/> property, contains assets 
    /// that are referenced by this asset.</li>
    /// <li><c>broken</c> dependencies: through the <see cref="BrokenLinksOut"/> property, 
    /// contains output links to assets that are missing.</li>
    /// </ul>
    /// </remarks>
    public class AssetDependencies
    {
        private readonly AssetItem item;
        private Dictionary<AssetId, AssetLink> parents;
        private Dictionary<AssetId, AssetLink> children;
        private Dictionary<AssetId, AssetLink> missingChildren;

        public AssetDependencies(AssetItem assetItem)
        {
            if (assetItem == null) throw new ArgumentNullException("assetItem");
            item = assetItem;
        }

        public AssetDependencies(AssetDependencies set)
        {
            if (set == null) throw new ArgumentNullException("set");
            item = set.Item;

            // Copy Output refs
            foreach (var child in set.LinksOut)
                AddLinkOut(child);

            // Copy Input refs
            foreach (var child in set.LinksIn)
                AddLinkIn(child);

            // Copy missing refs
            foreach (var child in set.BrokenLinksOut)
                AddBrokenLinkOut(child.Element, child.Type);
        }

        public AssetId Id
        {
            get
            {
                return item.Id;
            }
        }

        /// <summary>
        /// Gets the itemReferenced.
        /// </summary>
        /// <value>The itemReferenced.</value>
        public AssetItem Item
        {
            get
            {
                return item;
            }
        }

        /// <summary>
        /// Gets the links coming into the element.
        /// </summary>
        public IEnumerable<AssetLink> LinksIn
        {
            get
            {
                return parents != null? parents.Values: Enumerable.Empty<AssetLink>();
            }
        }

        /// <summary>
        /// Gets the links going out of the element.
        /// </summary>
        public IEnumerable<AssetLink> LinksOut
        {
            get
            {
                return children != null ? children.Values : Enumerable.Empty<AssetLink>();
            }
        }

        /// <summary>
        /// Gets the links out.
        /// </summary>
        /// <value>The missing references.</value>
        public IEnumerable<IContentLink> BrokenLinksOut
        {
            get
            {
                if (missingChildren == null)
                    yield break;

                foreach (var reference in missingChildren.Values)
                    yield return reference;
            }
        }

        /// <summary>
        /// Resets this instance and clear all dependencies (including missing)
        /// </summary>
        public void Reset(bool keepParents)
        {
            missingChildren = null;
            children = null;

            if (!keepParents) 
                parents = null;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has missing references.
        /// </summary>
        /// <value><c>true</c> if this instance has missing references; otherwise, 
        /// <c>false</c>.</value>
        public bool HasMissingDependencies
        {
            get
            {
                return missingChildren != null && missingChildren.Count > 0;
            }
        }

        /// <summary>
        /// Gets the number of missing dependencies of the asset.
        /// </summary>
        public int MissingDependencyCount
        {
            get
            {
                return missingChildren != null ? missingChildren.Count : 0;
            }
        }

        /// <summary>
        /// Adds a link going into the element.
        /// </summary>
        /// <param name="fromItem">The element the link is coming from</param>
        /// <param name="contentLinkType">The type of link</param>
        /// <exception cref="ArgumentException">A link from this element already exists</exception>
        public void AddLinkIn(AssetItem fromItem, ContentLinkType contentLinkType)
        {
            AddLink(ref parents, new AssetLink(fromItem, contentLinkType));
        }

        /// <summary>
        /// Adds a link coming from the provided element.
        /// </summary>
        /// <param name="contentLink">The link in</param>
        /// <exception cref="ArgumentException">A link from this element already exists</exception>
        public void AddLinkIn(AssetLink contentLink)
        {
            AddLink(ref parents, contentLink);
        }

        /// <summary>
        /// Gets the link coming from the provided element.
        /// </summary>
        /// <param name="fromItem">The element the link is coming from</param>
        /// <returns>The link</returns>
        /// <exception cref="ArgumentException">There is not link to the provided element</exception>
        /// <exception cref="ArgumentNullException">fromItem</exception>
        public AssetLink GetLinkIn(AssetItem fromItem)
        {
            if (fromItem == null) throw new ArgumentNullException("fromItem");

            return GetLink(ref parents, fromItem.Id);
        }

        /// <summary>
        /// Removes the link coming from the provided element.
        /// </summary>
        /// <param name="fromItem">The element the link is coming from</param>
        /// <exception cref="ArgumentNullException">fromItem</exception>
        /// <returns>The removed link</returns>
        public AssetLink RemoveLinkIn(AssetItem fromItem)
        {
            if (fromItem == null) throw new ArgumentNullException("fromItem");

            return RemoveLink(ref parents, fromItem.Id, ContentLinkType.All);
        }

        /// <summary>
        /// Adds a link going to the provided element.
        /// </summary>
        /// <param name="toItem">The element the link is going to</param>
        /// <param name="contentLinkType">The type of link</param>
        /// <exception cref="ArgumentException">A link to this element already exists</exception>
        public void AddLinkOut(AssetItem toItem, ContentLinkType contentLinkType)
        {
            AddLink(ref children, new AssetLink(toItem, contentLinkType));
        }

        /// <summary>
        /// Adds a link going to the provided element.
        /// </summary>
        /// <param name="contentLink">The link out</param>
        /// <exception cref="ArgumentException">A link to this element already exists</exception>
        public void AddLinkOut(AssetLink contentLink)
        {
            AddLink(ref children, contentLink);
        }

        /// <summary>
        /// Gets the link going to the provided element.
        /// </summary>
        /// <param name="toItem">The element the link is going to</param>
        /// <returns>The link</returns>
        /// <exception cref="ArgumentException">There is not link to the provided element</exception>
        /// <exception cref="ArgumentNullException">toItem</exception>
        public AssetLink GetLinkOut(AssetItem toItem)
        {
            if (toItem == null) throw new ArgumentNullException("toItem");

            return GetLink(ref children, toItem.Id);
        }

        /// <summary>
        /// Removes the link going to the provided element.
        /// </summary>
        /// <param name="toItem">The element the link is going to</param>
        /// <exception cref="ArgumentNullException">toItem</exception>
        /// <returns>The removed link</returns>
        public AssetLink RemoveLinkOut(AssetItem toItem)
        {
            if (toItem  == null) throw new ArgumentNullException("toItem");

            return RemoveLink(ref children, toItem.Id, ContentLinkType.All);
        }

        /// <summary>
        /// Adds a broken link out.
        /// </summary>
        /// <param name="reference">the reference to the missing element</param>
        /// <param name="contentLinkType">The type of link</param>
        /// <exception cref="ArgumentException">A broken link to this element already exists</exception>
        public void AddBrokenLinkOut(IReference reference, ContentLinkType contentLinkType)
        {
            AddLink(ref missingChildren, new AssetLink(reference, contentLinkType));
        }

        /// <summary>
        /// Adds a broken link out.
        /// </summary>
        /// <param name="contentLink">The broken link</param>
        /// <exception cref="ArgumentException">A broken link to this element already exists</exception>
        public void AddBrokenLinkOut(IContentLink contentLink)
        {
            AddLink(ref missingChildren, new AssetLink(contentLink.Element, contentLink.Type));
        }

        /// <summary>
        /// Gets the broken link out to the provided element.
        /// </summary>
        /// <param name="id">The id of the element the link is going to</param>
        /// <returns>The link</returns>
        /// <exception cref="ArgumentException">There is not link to the provided element</exception>
        /// <exception cref="ArgumentNullException">toItem</exception>
        public IContentLink GetBrokenLinkOut(AssetId id)
        {
            return GetLink(ref missingChildren, id);
        }

        /// <summary>
        /// Removes the broken link to the provided element.
        /// </summary>
        /// <param name="id">The id to the missing element</param>
        /// <exception cref="ArgumentNullException">toItem</exception>
        /// <returns>The removed link</returns>
        public IContentLink RemoveBrokenLinkOut(AssetId id)
        {
            return RemoveLink(ref missingChildren, id, ContentLinkType.All);
        }

        private void AddLink(ref Dictionary<AssetId, AssetLink> dictionary, AssetLink contentLink)
        {
            if (dictionary == null)
                dictionary = new Dictionary<AssetId, AssetLink>();

            var id = contentLink.Element.Id;
            if (dictionary.ContainsKey(id))
                contentLink.Type |= dictionary[id].Type;

            dictionary[id] = contentLink;
        }

        private AssetLink GetLink(ref Dictionary<AssetId, AssetLink> dictionary, AssetId id)
        {
            if (dictionary == null || !dictionary.ContainsKey(id))
                throw new ArgumentException("There is currently no link between elements '{0}' and '{1}'".ToFormat(item.Id, id));

            return dictionary[id];
        }

        private AssetLink RemoveLink(ref Dictionary<AssetId, AssetLink> dictionary, AssetId id, ContentLinkType type)
        {
            if (dictionary == null || !dictionary.ContainsKey(id))
                throw new ArgumentException("There is currently no link between elements '{0}' and '{1}'".ToFormat(item.Id, id));

            var oldLink = dictionary[id];
            var newLink = oldLink;

            newLink.Type &= ~type;
            oldLink.Type &= type;

            if (newLink.Type == 0)
                dictionary.Remove(id);

            if (dictionary.Count == 0)
                dictionary = null;

            return oldLink;
        }
    }
}
