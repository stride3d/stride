// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Shaders.Ast;
using System.Linq;

namespace Stride.Core.Shaders.Visitor
{
    /// <summary>
    /// Visitor base.
    /// </summary>
    public abstract class VisitorBase
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VisitorBase"/> class.
        /// </summary>
        /// <param name="useNodeStack">if set to <c>true</c> [use node stack].</param>
        protected VisitorBase(bool useNodeStack = false)
        {
            if (useNodeStack)
            {
                NodeStack = new List<Node>();
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the node stack.
        /// </summary>
        /// <value>
        /// The node stack.
        /// </value>
        public List<Node> NodeStack { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Visits the list.
        /// </summary>
        /// <typeparam name="T">Type of the item in the list</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="filter">The function filter.</param>
        protected void VisitList<T>(IList<T> list, Func<T, bool> filter = null) where T : Node
        {
            if (list == null)
                return;

            int i = 0;
            while (i < list.Count)
            {
                var previousValue = (Node)list[i];
                var temp = VisitDynamic(previousValue);

                // Recover the position as the list can be modified while processing a node
                for (i = 0; i < list.Count; i++)
                {
                    if (ReferenceEquals(previousValue, list[i]))
                        break;
                }

                if (temp == null)
                {
                    list.RemoveAt(i);
                }
                else
                {
                    if (!ReferenceEquals(previousValue, temp))
                        list[i] = (T)temp;
                    i++;
                }
            }
        }

        /// <summary>
        /// Visits the node.
        /// </summary>
        /// <typeparam name="T">Type of the node</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="visitRealType">if set to <c>true</c> [visit real type].</param>
        /// <returns>
        /// A node
        /// </returns>
        protected virtual Node VisitDynamic(Node node)
        {
            if (node == null)
            {
                return null;
            }

            bool nodeStackAdded = false;

            if (NodeStack != null)
            {
                if (NodeStack.Count > 0 && ReferenceEquals(NodeStack[NodeStack.Count - 1], node))
                    throw new InvalidOperationException(string.Format("Cannot visit recursively a node [{0}] already being visited", node));

                NodeStack.Add(node);
                nodeStackAdded = true;
            }

            // Only Visit in the Iterator
            bool doVisit = PreVisitNode(node);

            var result = (Node)node;
            if (doVisit)
            {
                result = DoVisitNode(node);
            }

            // Only Visit in the Iterator
            PostVisitNode(node, doVisit);

            if (NodeStack != null && nodeStackAdded)
            {
                NodeStack.RemoveAt(NodeStack.Count - 1);
            }

            return result;
        }

        protected abstract Node DoVisitNode(Node node);

        #endregion

        protected readonly NodeProcessor nodeProcessor;

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderVisitor"/> class.
        /// </summary>
        /// <param name="buildScopeDeclaration">if set to <c>true</c> [build scope declaration].</param>
        /// <param name="useNodeStack">if set to <c>true</c> [use node stack].</param>
        protected VisitorBase(bool buildScopeDeclaration, bool useNodeStack) : this(useNodeStack)
        {
            if (buildScopeDeclaration)
            {
                ScopeStack = new Stack<ScopeDeclaration>();
                ScopeStack.Push(this.NewScope());
            }
        }

        #endregion

        #region Properties

        protected virtual ScopeDeclaration NewScope(IScopeContainer container = null)
        {
            return new ScopeDeclaration(container);
        }

        /// <summary>
        /// Gets the parent node or null if no parents
        /// </summary>
        public Node ParentNode
        {
            get
            {
                return NodeStack.Count > 1 ? NodeStack[NodeStack.Count - 2] : null;
            }
        }

        /// <summary>
        /// Gets the scope stack.
        /// </summary>
        protected Stack<ScopeDeclaration> ScopeStack { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds a list of declaration by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A list of declaration</returns>
        protected virtual IEnumerable<IDeclaration> FindDeclarations(string name)
        {
            return ScopeStack.SelectMany(scopeDecl => scopeDecl.FindDeclaration(name));
        }

        /// <summary>
        /// Finds a declaration by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A declaration</returns>
        protected IDeclaration FindDeclaration(string name)
        {
            return FindDeclarations(name).FirstOrDefault();
        }

        /// <summary>
        /// Called before visiting the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>True to continue visiting the node; false to skip the visit</returns>
        protected virtual bool PreVisitNode(Node node)
        {
            if (ScopeStack != null)
            {
                var declaration = node as IDeclaration;
                if (declaration != null)
                {
                    // If this is a variable, add only instance variables
                    if (declaration is Variable)
                    {
                        foreach (var variable in ((Variable)declaration).Instances())
                            ScopeStack.Peek().AddDeclaration(variable);
                    }
                    else
                    {
                        ScopeStack.Peek().AddDeclaration(declaration);
                    }
                }

                var scopeContainer = node as IScopeContainer;
                if (scopeContainer != null)
                {
                    ScopeStack.Push(this.NewScope(scopeContainer));
                }
            }
            return true;
        }

        /// <summary>
        /// Called after visiting the node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="nodeVisited">if set to <c>true</c> [node visited].</param>
        protected virtual void PostVisitNode(Node node, bool nodeVisited)
        {
            if (ScopeStack != null && node is IScopeContainer)
                ScopeStack.Pop();
        }

        #endregion

    }
}
