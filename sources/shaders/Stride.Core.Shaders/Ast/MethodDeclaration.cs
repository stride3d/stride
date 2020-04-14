// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Declaration of a method.
    /// </summary>
    public partial class MethodDeclaration : Node, IDeclaration, IAttributes, IQualifiers, IScopeContainer
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "MethodDeclaration" /> class.
        /// </summary>
        public MethodDeclaration()
        {
            Attributes = new List<AttributeBase>();
            Parameters = new List<Parameter>();
            Qualifiers = Qualifier.None;
            ParameterConstraints = new List<GenericParameterConstraint>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the attributes.
        /// </summary>
        /// <value>
        ///   The attributes.
        /// </value>
        /// <inhericdoc />
        public List<AttributeBase> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        /// Gets or sets the parameter constraints.
        /// </summary>
        /// <value>
        /// The parameter constraints.
        /// </value>
        public List<GenericParameterConstraint> ParameterConstraints { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public List<Parameter> Parameters { get; set; }

        /// <summary>
        ///   Gets or sets the storage class.
        /// </summary>
        /// <value>
        ///   The storage class.
        /// </value>
        public Qualifier Qualifiers { get; set; }

        /// <summary>
        /// Gets or sets the type of the return.
        /// </summary>
        /// <value>
        /// The type of the return.
        /// </value>
        public TypeBase ReturnType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is builtin.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is builtin; otherwise, <c>false</c>.
        /// </value>
        public bool IsBuiltin { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks the constraint.
        /// </summary>
        /// <param name="parameterType">Type of the parameter.</param>
        /// <param name="typeToCheck">The type to check.</param>
        /// <returns></returns>
        public bool CheckConstraint(GenericParameterType parameterType, TypeBase typeToCheck)
        {
            foreach (var genericParameterConstraint in ParameterConstraints)
            {
                if (genericParameterConstraint.Name == parameterType.Name)
                {
                    return genericParameterConstraint.Constraint(typeToCheck);
                }
            }
            return false;
        }

        /// <summary>
        /// Test if a method declaration has the same signature.
        /// </summary>
        /// <param name="methodDeclaration">The method declaration.</param>
        /// <returns>True if the method passed has the same signature</returns>
        public bool IsSameSignature(MethodDeclaration methodDeclaration)
        {
            if (methodDeclaration == null)
                return false;

            if (Name != methodDeclaration.Name)
                return false;
            if (Parameters.Count != methodDeclaration.Parameters.Count)
                return false;
            for (int i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                var parameterAgainst = methodDeclaration.Parameters[i];
                var parameterType = parameter.Type.ResolveType();
                var parameterAgainstType = parameterAgainst.Type.ResolveType();
                if (parameterType != parameterAgainstType)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Test if a method invocation expression has the same signature.
        /// </summary>
        /// <param name="methodInvocationExpression">The method invocation expression.</param>
        /// <returns>True if the method passed has the same signature</returns>
        public bool IsSameSignature(MethodInvocationExpression methodInvocationExpression)
        {
            if (methodInvocationExpression == null)
                return false;

            Identifier methodName;
            var target = methodInvocationExpression.Target as MemberReferenceExpression;
            if (target != null)
                methodName = target.Member;
            else
            {
                var vre = methodInvocationExpression.Target as VariableReferenceExpression;
                if (vre == null)
                    return false;
                methodName = vre.Name;
            }

            if (Name != methodName)
                return false;
            if (Parameters.Count != methodInvocationExpression.Arguments.Count)
                return false;
            for (int i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                var parameterAgainst = methodInvocationExpression.Arguments[i];
                var parameterType = parameter.Type.ResolveType();
                
                if (parameterAgainst.TypeInference.TargetType == null)
                    return false;

                var parameterAgainstType = parameterAgainst.TypeInference.TargetType.ResolveType();
                if (parameterType != parameterAgainstType)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Copies declartion to another instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        public void CopyTo(MethodDeclaration target)
        {
            target.Attributes = Attributes;
            target.Name = Name;
            target.Parameters = Parameters;
            target.Qualifiers = Qualifiers;
            target.ReturnType = ReturnType;
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(ReturnType);
            ChildrenList.Add(Name);
            foreach (var variableDeclarator in Parameters)
            {
                ChildrenList.Add(variableDeclarator);
            }
            if (Qualifiers != Qualifier.None)
            {
                ChildrenList.Add(Qualifiers);
            }

            return ChildrenList;
        }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                "{0}{1} {2}({3}){4}",                 
                Qualifiers == Qualifier.None ? string.Empty : Qualifiers + " ",
                ReturnType,
                Name,
                string.Join(",", Parameters),
                GetType() == typeof(MethodDeclaration) ? ";" : " {...}");
        }

        /// <inheritdoc />
        /*public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (ReturnType != null ? ReturnType.GetHashCode() : 0);
            }
        }*/

        #endregion

        #region Methods

        internal void UpdateParameters()
        {
            foreach (var parameter in Parameters)
            {
                parameter.DeclaringMethod = this;
            }
        }

        #endregion
    }
}
