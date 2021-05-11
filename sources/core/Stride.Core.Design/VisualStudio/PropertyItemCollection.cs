#region License

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.
//
// SLNTools
// Copyright (c) 2009
// by Christian Warren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Stride.Core.Annotations;

namespace Stride.Core.VisualStudio
{
    /// <summary>
    /// A collection of <see cref="PropertyItem"/>
    /// </summary>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class PropertyItemCollection
        : KeyedCollection<string, PropertyItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItemCollection"/> class.
        /// </summary>
        public PropertyItemCollection()
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyItemCollection"/> class.
        /// </summary>
        /// <param name="items">The items to copy from.</param>
        public PropertyItemCollection(IEnumerable<PropertyItem> items)
            : this()
        {
            this.AddRange(items);
        }

        protected override string GetKeyForItem([NotNull] PropertyItem item)
        {
            return item.Name;
        }

        protected override void InsertItem(int index, [NotNull] PropertyItem item)
        {
            var existingItem = (Contains(GetKeyForItem(item))) ? this[GetKeyForItem(item)] : null;

            if (existingItem == null)
            {
                // Add a clone of the item instead of the item itself
                base.InsertItem(index, item.Clone());
            }
            else if (item.Value != existingItem.Value)
            {
                existingItem.Value = item.Value;
            }
        }

        protected override void SetItem(int index, [NotNull] PropertyItem item)
        {
            // Add a clone of the item instead of the item itself
            base.SetItem(index, item.Clone());
        }
    }
}