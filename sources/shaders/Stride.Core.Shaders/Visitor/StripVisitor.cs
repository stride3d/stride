// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;

namespace Stride.Core.Shaders.Visitor
{
    /// <summary>
    /// The strip visitor collects all function and declaration used by a set of entrypoints
    /// and remove any unreferenced/unused declaration.
    /// </summary>
    public class StripVisitor : ShaderWalker
    {
        private Dictionary<Node, HashSet<Node>> indirectReferences;
        private readonly string[] entryPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="StripVisitor"/> class.
        /// </summary>
        /// <param name="entryPoints">The entry points to filter.</param>
        public StripVisitor(params string[] entryPoints) : base(true, true)
        {
            this.entryPoints = entryPoints;
            this.StripUniforms = true;
            this.KeepConstantBuffers = true;
        }

        public bool StripUniforms { get; set; }

        public bool KeepConstantBuffers { get; set; }

        public override void Visit(MethodInvocationExpression methodInvocationExpression)
        {
            base.Visit(methodInvocationExpression);
            AddReference(GetDeclarationContainer(), (Node)methodInvocationExpression.TypeInference.Declaration);
        }

        public override void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            base.Visit(variableReferenceExpression);
            AddReference(GetDeclarationContainer(), (Node)variableReferenceExpression.TypeInference.Declaration);
        }

        private ConstantBuffer currentConstantBuffer = null;

        public override void Visit(ConstantBuffer constantBuffer)
        {
            currentConstantBuffer = constantBuffer;
            base.Visit(constantBuffer);
            currentConstantBuffer = null;
        }

        protected override bool PreVisitNode(Node node)
        {
            // Sometimes it is desirable that constant buffer are not modified so that
            // they can be shared between different stages, even if some variables are unused.
            // In this case, use symetric reference so that using a constant buffer will include all its variables.
            if (KeepConstantBuffers && currentConstantBuffer != null && node is IDeclaration)
            {
                AddReference(node, currentConstantBuffer);
                AddReference(currentConstantBuffer, node);
            }

            return base.PreVisitNode(node);

        }

        public override void Visit(Parameter parameter)
        {
            base.Visit(parameter);
            var containers = GetDeclarationContainers();
            var container = containers[containers.Count - 2];
            AddReference((Node)container, parameter);
        }

        public override void DefaultVisit(Node node)
        {
            base.DefaultVisit(node);

            var typeBase = node as TypeBase;
            if (typeBase != null)
                AddReference(GetDeclarationContainer(), (Node)typeBase.TypeInference.Declaration);
        }

        public override void Visit(MethodDefinition methodDefinition)
        {
            base.Visit(methodDefinition);

            // If a method definition has a method declaration, we must link them together
            if (!ReferenceEquals(methodDefinition.Declaration, methodDefinition))
            {
                AddReference(methodDefinition.Declaration, methodDefinition);
            }
        }

        public override void Visit(Variable variable)
        {
            base.Visit(variable);
            var containers = GetDeclarationContainers();
            if (containers.Count > 1)
            {
                var container = containers[containers.Count - 2];
                AddReference((Node)container, variable);
            }
        }
        
        public override void Visit(Shader shader)
        {
            indirectReferences = new Dictionary<Node, HashSet<Node>>();

            // Visit AST.
            base.Visit( shader);

            // Get list of function referenced (directly or indirectly) by entry point.
            // Using hashset and recursion to avoid cycle.
            var collectedReferences = new List<Node>();
            foreach (var entryPointName in entryPoints)
            {
                // Find entry point
                var entryPoint = shader.Declarations.OfType<MethodDefinition>().FirstOrDefault(x => x.Name == entryPointName);

                if (entryPoint == null)
                    throw new ArgumentException(string.Format("Could not find entry point named {0}", entryPointName));

                CollectReferences(collectedReferences, entryPoint);
            }

            StripDeclarations(shader.Declarations, collectedReferences, StripUniforms);
        }

        /// <summary>
        /// Strips the declarations.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="collectedReferences">The collected references.</param>
        private static void StripDeclarations(IList<Node> nodes, ICollection<Node> collectedReferences, bool stripUniforms)
        {
            // Remove all the unreferenced function amd types declaration from the shader.
            for (int i = 0; i < nodes.Count; i++)
            {
                var declaration = nodes[i];

                // Strip constant buffer elements by elements only if "stripUniforms" is active (useful for API without constant buffers like OpenGL ES 2.0)
                if (stripUniforms && declaration is ConstantBuffer)
                {
                    var constantBuffer = (ConstantBuffer)declaration;
                    StripDeclarations(constantBuffer.Members, collectedReferences, stripUniforms);
                }

                if (CanStrip(declaration, collectedReferences, stripUniforms))
                {
                    nodes.RemoveAt(i--);
                }
            }
        }

        private static bool CanStrip(Node declaration, ICollection<Node> collectedReferences, bool stripUniforms)
        {
            if (declaration is Variable)
            {
                var variableDeclaration = (Variable)declaration;
                if ((!stripUniforms && variableDeclaration.Qualifiers.Contains(Ast.StorageQualifier.Uniform)))
                    return false;

                if (variableDeclaration.IsGroup)
                {
                    variableDeclaration.SubVariables.RemoveAll(x => !collectedReferences.Contains(x));
                    if (variableDeclaration.SubVariables.Count == 0)
                    {
                        return true;
                    }
                }
                else if (!collectedReferences.Contains(declaration))
                {
                    return true;
                }
            }
            else if (declaration is IDeclaration && !collectedReferences.Contains(declaration))
            {
                return true;
            }
            else if (declaration is ConstantBuffer)
            {
                // Strip cbuffer only if all of its member can be
                var constantBuffer = (ConstantBuffer)declaration;
                foreach (var member in constantBuffer.Members)
                {
                    if (!CanStrip(member, collectedReferences, stripUniforms))
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Helper to collects the referenced declarations recursively.
        /// </summary>
        /// <param name="collectedReferences">The collected declarations.</param>
        /// <param name="reference">The reference to collect.</param>
        private void CollectReferences(List<Node> collectedReferences, Node reference)
        {
            if (!collectedReferences.Contains(reference))
            {
                // Collect reference (if not already added)
                collectedReferences.Add(reference);

                // Collect recursively
                HashSet<Node> referencedFunctions;
                if (indirectReferences.TryGetValue((Node)reference, out referencedFunctions))
                {
                    foreach (var referencedFunction in referencedFunctions)
                        CollectReferences(collectedReferences, referencedFunction);
                }
            }
        }

        private void AddReference(Node parent, Node declaration)
        {
            if (parent != null && declaration != null)
            {
                HashSet<Node> childReferences;
                if (!indirectReferences.TryGetValue(parent, out childReferences))
                {
                    childReferences = new HashSet<Node>();
                    indirectReferences[parent] = childReferences;
                }
                if (!childReferences.Contains(declaration))
                    childReferences.Add(declaration);
            }
        }


        private Node GetDeclarationContainer()
        {
            // By default use the method definition as the main declarator container
            var methodDefinition = (Node)NodeStack.OfType<MethodDefinition>().LastOrDefault();
            if (methodDefinition != null)
                return methodDefinition;

            // Else use the IDeclaration
            return (Node)NodeStack.OfType<IDeclaration>().LastOrDefault();
        }

        private List<IDeclaration> GetDeclarationContainers()
        {
            return NodeStack.OfType<IDeclaration>().ToList();
        }
    }
}

