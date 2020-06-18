// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A collection of <see cref="AssetFolder"/>
    /// </summary>
    [DataContract("AssetFolderCollection")]
    [DebuggerTypeProxy(typeof(CollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class AssetFolderCollection : IList<AssetFolder>, IReadOnlyList<AssetFolder>
    {
        private readonly List<AssetFolder> folders;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetFolderCollection"/> class.
        /// </summary>
        public AssetFolderCollection()
        {
            folders = new List<AssetFolder>();
        }

        /// <inheritdoc/>
        public void Add(AssetFolder item)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (item.Path == null)
            {
                throw new ArgumentOutOfRangeException("item", "SourceFolder.Folder cannot be null");
            }

            // If a folder is already added, use it and squash the item to add to the existing folder.
            var existingFolder = Find(item.Path);
            if (existingFolder == null)
            {
                folders.Add(item);
            }
        }

        public AssetFolder Find(UDirectory folder)
        {
            return folders.FirstOrDefault(sourceFolder => sourceFolder.Path == folder);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            folders.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(AssetFolder item)
        {
            if (item == null) throw new ArgumentNullException("item");
            return folders.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(AssetFolder[] array, int arrayIndex)
        {
            folders.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Clones this instance to the specified instance.
        /// </summary>
        /// <param name="foldersTo">The folders.</param>
        /// <exception cref="System.ArgumentNullException">folders</exception>
        public void CloneTo(AssetFolderCollection foldersTo)
        {
            if (foldersTo == null) throw new ArgumentNullException("folders");
            foreach (var sourceFolder in this)
            {
                foldersTo.Add(sourceFolder.Clone());
            }
        }

        /// <inheritdoc/>
        public bool Remove(AssetFolder item)
        {
            if (item == null) throw new ArgumentNullException("item");
            return folders.Remove(item);
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                return folders.Count;
            }
        }

        /// <inheritdoc/>
        bool ICollection<AssetFolder>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public AssetFolder this[int index] => folders[index];

        /// <inheritdoc/>
        public IEnumerator<AssetFolder> GetEnumerator()
        {
            return folders.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)folders).GetEnumerator();
        }

        int IList<AssetFolder>.IndexOf(AssetFolder item)
        {
            return folders.IndexOf(item);
        }

        void IList<AssetFolder>.Insert(int index, AssetFolder item)
        {
            folders.Insert(index, item);
        }

        void IList<AssetFolder>.RemoveAt(int index)
        {
            folders.RemoveAt(index);
        }

        AssetFolder IList<AssetFolder>.this[int index] { get { return folders[index]; } set { folders[index] = value; } }
    }
}
