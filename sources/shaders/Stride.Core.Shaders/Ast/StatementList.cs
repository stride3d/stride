// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A list of statement.
    /// </summary>
    /// <remarks>
    /// This class can be use to expand codes as a replacement in visitors.
    /// </remarks>
    public partial class StatementList : Statement, IList<Statement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatementList"/> class.
        /// </summary>
        public StatementList()
        {
            Statements = new List<Statement>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatementList"/> class.
        /// </summary>
        /// <param name="statements">The statements.</param>
        public StatementList(params Statement [] statements)
        {
            Statements = new List<Statement>();
            if (statements != null)
                Statements.AddRange(statements);
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                return Statements.Count;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the statements.
        /// </summary>
        /// <value>
        /// The statements.
        /// </value>
        public List<Statement> Statements { get; set; }

        /// <summary>
        /// Adds a collection to this instance.
        /// </summary>
        /// <param name="collection">The collection to add to this instance.</param>
        public void AddRange(IEnumerable<Statement> collection)
        {
            Statements.AddRange(collection);
        }

        /// <summary>
        /// Gets a subset of this instance
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <returns>A subset of this instance</returns>
        public List<Statement> GetRange(int index, int count)
        {
            return Statements.GetRange(index, count);
        }

        /// <summary>
        /// Inserts a collection at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="collection">The collection.</param>
        public void InsertRange(int index, IEnumerable<Statement> collection)
        {
            Statements.InsertRange(index, collection);
        }

        /// <summary>
        /// Removes a range of elements.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        public void RemoveRange(int index, int count)
        {
            Statements.RemoveRange(index, count);            
        }

        /// <summary>
        /// Removes all elements with a predicate function.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns>Number of elements removed</returns>
        public int RemoveAll(Predicate<Statement> match)
        {
            return Statements.RemoveAll(match);
        }

        /// <inheritdoc/>
        public Statement this[int index]
        {
            get
            {
                return Statements[index];
            }

            set
            {
                Statements[index] = value;
            }
        }

        /// <inheritdoc/>
        public void Add(Statement item)
        {
            Statements.Add(item);
        }

        public override IEnumerable<Node> Childrens()
        {
            return this;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            Statements.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(Statement item)
        {
            return Statements.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(Statement[] array, int arrayIndex)
        {
            Statements.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<Statement> GetEnumerator()
        {
            return Statements.GetEnumerator();
        }

        /// <inheritdoc/>
        public int IndexOf(Statement item)
        {
            return Statements.IndexOf(item);
        }

        /// <inheritdoc/>
        public void Insert(int index, Statement item)
        {
            Statements.Insert(index, item);
        }

        /// <inheritdoc/>
        public bool Remove(Statement item)
        {
            return Statements.Remove(item);
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            Statements.RemoveAt(index);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
