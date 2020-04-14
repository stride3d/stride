// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core;
using Stride.Core.Shaders.Visitor;
using SourceSpan = Stride.Core.Shaders.Ast.SourceSpan;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Abstract node.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class Node
    {
        /// <summary>
        /// list of childrens for ast navigation.
        /// </summary>
        private List<Node> childrenList = null;
        private Dictionary<string, object> tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        protected Node()
        {
        }

        /// <summary>
        /// Gets or sets the source span.
        /// </summary>
        /// <value>
        /// The source span.
        /// </value>
        public SourceSpan Span { get; set; }


        public override bool Equals(object against)
        {
            return base.Equals(against);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Node left, Node right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Node left, Node right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Gets the childrens.
        /// </summary>
        [DataMemberIgnore]
        [VisitorIgnore]
        protected List<Node> ChildrenList
        {
            get
            {
                if (childrenList == null)
                    childrenList = new List<Node>();
                return childrenList;
            }
        }

        /// <summary>
        /// Gets or sets tags collection.
        /// </summary>
        public Dictionary<string, object> Tags
        {
            get { return tags; }
            set { tags = value; }
        }

        /// <summary>
        /// Gets a tag value associated to this node..
        /// </summary>
        /// <param name="tagKey">The tag key.</param>
        /// <returns>The tag value</returns>
        public object GetTag(string tagKey)
        {
            if (tags == null) return null;
            object result;
            tags.TryGetValue(tagKey, out result);
            return result;
        }

        /// <summary>
        /// Gets a tag value associated to this node..
        /// </summary>
        /// <param name="tagKey">The tag key.</param>
        /// <returns>The tag value</returns>
        public bool RemoveTag(string tagKey)
        {
            if (tags == null) return true;
            return tags.Remove(tagKey);
        }

        /// <summary>
        /// Determines whether the specified instance contains this tag.
        /// </summary>
        /// <param name="tagKey">The tag key.</param>
        /// <returns>
        ///   <c>true</c> if the specified instance contains this tag; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsTag(string tagKey)
        {
            if (tags == null) return false;
            return tags.ContainsKey(tagKey);
        }

        /// <summary>
        /// Sets a tag value associated to this node.
        /// </summary>
        /// <param name="tagKey">The tag key.</param>
        /// <param name="tagValue">The tag value.</param>
        public void SetTag(string tagKey, object tagValue)
        {
            if (tags == null) tags = new Dictionary<string, object>();
            tags.Remove(tagKey);
            tags.Add(tagKey, tagValue);
        }

        /// <summary>
        /// Gets the child nodes.
        /// </summary>
        /// <returns>An enumeration of child nodes</returns>
        public virtual IEnumerable<Node> Childrens()
        {
            return ChildrenList;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return GetType().Name;
        }

        public abstract void Accept(ShaderVisitor visitor);

        public abstract TResult Accept<TResult>(ShaderVisitor<TResult> visitor);
    }
}
