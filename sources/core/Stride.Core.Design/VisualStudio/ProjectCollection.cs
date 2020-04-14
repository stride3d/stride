#region License

// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.VisualStudio
{
    /// <summary>
    /// A collection of <see cref="Project"/>
    /// </summary>
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class ProjectCollection : KeyedCollection<Guid, Project>
    {
        private readonly Solution solution;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectCollection"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <exception cref="System.ArgumentNullException">container</exception>
        internal ProjectCollection([NotNull] Solution container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            solution = container;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectCollection"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="items">The items.</param>
        internal ProjectCollection([NotNull] Solution container, IEnumerable<Project> items)
            : this(container)
        {
            this.AddRange(items);
        }

        /// <summary>
        /// Gets the solution this project is attached to.
        /// </summary>
        /// <value>The solution.</value>
        public Solution Solution
        {
            get
            {
                return solution;
            }
        }

        /// <summary>
        /// Finds a project by its full name.
        /// </summary>
        /// <param name="projectFullName">Full name of the project.</param>
        /// <returns>The Project or null if not found.</returns>
        [CanBeNull]
        public Project FindByFullName(string projectFullName)
        {
            return this.FirstOrDefault(item => string.Compare(item.GetFullName(solution), projectFullName, StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        /// <summary>
        /// Finds a project by its unique identifier.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>The project or null if not found.</returns>
        [CanBeNull]
        public Project FindByGuid(Guid guid)
        {
            return (Contains(guid)) ? this[guid] : null;
        }

        /// <summary>
        /// Sorts this instance.
        /// </summary>
        public void Sort()
        {
            Sort((p1, p2) => StringComparer.InvariantCultureIgnoreCase.Compare(p1.GetFullName(solution), p2.GetFullName(solution)));
        }

        public void Sort([NotNull] Comparison<Project> comparer)
        {
            var tempList = new List<Project>(this);
            tempList.Sort(comparer);

            Clear();
            this.AddRange(tempList);
        }

        protected override Guid GetKeyForItem([NotNull] Project item)
        {
            return item.Guid;
        }
    }
}