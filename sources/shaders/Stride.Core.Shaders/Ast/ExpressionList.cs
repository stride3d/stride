// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A list of expression.
    /// </summary>
    public partial class ExpressionList : Expression, IList<Expression>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionList"/> class.
        /// </summary>
        public ExpressionList()
        {
            Expressions = new List<Expression>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionList"/> class.
        /// </summary>
        /// <param name="expressions">The expressions.</param>
        public ExpressionList(params Expression [] expressions)
        {
            Expressions = new List<Expression>();
            if (expressions != null)
                Expressions.AddRange(expressions);
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                return Expressions.Count;
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
        /// Gets or sets the expressions.
        /// </summary>
        /// <value>
        /// The expressions.
        /// </value>
        public List<Expression> Expressions { get; set; }

        /// <summary>
        /// Adds a collection to this instance.
        /// </summary>
        /// <param name="collection">The collection to add to this instance.</param>
        public void AddRange(IEnumerable<Expression> collection)
        {
            Expressions.AddRange(collection);
        }

        /// <summary>
        /// Gets a subset of this instance
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        /// <returns>A subset of this instance</returns>
        public List<Expression> GetRange(int index, int count)
        {
            return Expressions.GetRange(index, count);
        }

        /// <summary>
        /// Inserts a collection at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="collection">The collection.</param>
        public void InsertRange(int index, IEnumerable<Expression> collection)
        {
            Expressions.InsertRange(index, collection);
        }

        /// <summary>
        /// Removes a range of elements.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="count">The count.</param>
        public void RemoveRange(int index, int count)
        {
            Expressions.RemoveRange(index, count);            
        }

        /// <summary>
        /// Removes all elements with a predicate function.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns>Number of elements removed</returns>
        public int RemoveAll(Predicate<Expression> match)
        {
            return Expressions.RemoveAll(match);
        }

        /// <inheritdoc/>
        public Expression this[int index]
        {
            get
            {
                return Expressions[index];
            }

            set
            {
                Expressions[index] = value;
            }
        }

        /// <inheritdoc/>
        public void Add(Expression item)
        {
            Expressions.Add(item);
        }

        public override IEnumerable<Node> Childrens()
        {
            return this;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            Expressions.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(Expression item)
        {
            return Expressions.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(Expression[] array, int arrayIndex)
        {
            Expressions.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<Expression> GetEnumerator()
        {
            return Expressions.GetEnumerator();
        }

        /// <inheritdoc/>
        public int IndexOf(Expression item)
        {
            return Expressions.IndexOf(item);
        }

        /// <inheritdoc/>
        public void Insert(int index, Expression item)
        {
            Expressions.Insert(index, item);
        }

        /// <inheritdoc/>
        public bool Remove(Expression item)
        {
            return Expressions.Remove(item);
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            Expressions.RemoveAt(index);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return string.Join(", ", this);
        }
    }
}
