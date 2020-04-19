// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// Collect a list of global uniforms that are used as global temporary variable.
    /// </summary>
    public class GlobalUniformVisitor : CallstackVisitor
    {
        private List<Variable> uniformReadList;

        private Shader shader;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalUniformVisitor"/> class.
        /// </summary>
        /// <param name="shader">The shader.</param>
        public GlobalUniformVisitor(Shader shader)
        {
            this.shader = shader;
            uniformReadList = new List<Variable>();
            this.UniformUsedWriteFirstList = new List<Variable>();
            this.UniformReadWriteList = new List<Variable>();
        }

        /// <summary>
        /// Gets a list of uniform that are used as "write" variable first.
        /// </summary>
        public List<Variable> UniformUsedWriteFirstList { get; private set; }

        /// <summary>
        /// Gets a list of uniform that are used as "read" and "write" variable.
        /// </summary>
        public List<Variable> UniformReadWriteList { get; private set; }

        public bool IsVariableAsGlobalTemporary(Variable variable)
        {
            return UniformUsedWriteFirstList.Contains(variable);
        }

        public bool IsVariableAsGlobalTemporary(Expression expression)
        {
            var variable = GetUniform(expression);
            if (variable == null)
                return false;
            return IsVariableAsGlobalTemporary(variable);
        }

        public bool IsUniformReadWrite(Variable variable)
        {
            return UniformReadWriteList.Contains(variable);
        }

        public bool IsUniformReadWrite(Expression expression)
        {
            var variable = GetUniform(expression);
            if (variable == null)
                return false;
            return IsUniformReadWrite(variable);
        }

        public override Node Visit(VariableReferenceExpression variableRef)
        {
            var variable = GetUniform(variableRef);

            // If the variable is a global uniform, non static/const and is not already in the list used then
            if (variable != null && !uniformReadList.Contains(variable))
            {
                uniformReadList.Add(variable);
            }
            return variableRef;
        }

        private Variable GetUniform(Expression expression)
        {
            VariableReferenceExpression variableRef = null;
            while (expression != null)
            {
                if (expression is MemberReferenceExpression)
                {
                    expression = ((MemberReferenceExpression)expression).Target;
                }
                else if (expression is IndexerExpression)
                {
                    expression = ((IndexerExpression)expression).Target;
                }
                else
                {
                    variableRef = expression as VariableReferenceExpression;
                    break;
                }
            }

            if (variableRef != null)
            {
                var variable = variableRef.TypeInference.Declaration as Variable;

                // If the variable is a global uniform, non static/const and is not already in the list used then
                return (variable != null && shader.Declarations.Contains(variable) && !variable.Qualifiers.Contains(Ast.Hlsl.StorageQualifier.Static)
                        && !variable.Qualifiers.Contains(Ast.StorageQualifier.Const))
                           ? variable
                           : null;
            }
            return null;
        }

        private int countReadBeforeInvoke;

        public override Node Visit(MethodInvocationExpression methodInvocationExpression)
        {
            // Save the number of variable in read-only mode
            countReadBeforeInvoke = uniformReadList.Count;
            return base.Visit(methodInvocationExpression);
        }

        protected override void ProcessMethodInvocation(MethodInvocationExpression invoke, MethodDefinition method)
        {

            // Handle the case where a parameter can be out
            // If this is the case, we need to check that 
            for (int i = 0; i < invoke.Arguments.Count; i++)
            {
                var arg = invoke.Arguments[i];
                var variable = this.GetUniform(arg);
                var parameter = method.Parameters[i];
                if (variable != null && parameter.Qualifiers.Contains(ParameterQualifier.Out))
                {
                    bool isUniformWasAlreadyUsedAsRead = false;
                    for (int j = 0; j < countReadBeforeInvoke; j++)
                    {
                        if (ReferenceEquals(uniformReadList[i], variable))
                        {
                            isUniformWasAlreadyUsedAsRead = true;
                            break;
                        }
                    }

                    // If this is a out parameter, and the variable was not already used as a read, then
                    // we can remove it from the uniform read list
                    if (!isUniformWasAlreadyUsedAsRead)
                    {
                        uniformReadList.Remove(variable);
                        if (!UniformUsedWriteFirstList.Contains(variable))
                            UniformUsedWriteFirstList.Add(variable);
                    }
                }
            }

            this.VisitDynamic(method);
        }

        public override Node Visit(AssignmentExpression assignmentExpression)
        {
            var variable = GetUniform(assignmentExpression.Target);
            bool isMemberExpression = assignmentExpression.Target is MemberReferenceExpression;
            if (variable != null)
            {
                // Default == operator is the only write only operators
                if (assignmentExpression.Operator == AssignmentOperator.Default && !uniformReadList.Contains(variable) && !this.UniformUsedWriteFirstList.Contains(variable))
                {
                    // Handle the case where the assignment operator is partial like vect.xy = 5; and later vect.zw += 6;
                    // In this case, the variable is considered as read and write (and not only write first)
                    if (isMemberExpression)
                    {
                        var variableType = variable.Type.ResolveType();

                        if (variableType is VectorType || variableType is MatrixType)
                        {
                            var dim = Math.Max(TypeBase.GetDimensionSize(variableType, 0), TypeBase.GetDimensionSize(variableType, 1));

                            var memberRef = assignmentExpression.Target as MemberReferenceExpression;
                            var numberOfMembers = memberRef.Member.Text.Length;

                            // If the variable is a global uniform, non static/const and is not already in the list used then
                            if (numberOfMembers < dim)
                            {
                                if (!uniformReadList.Contains(variable))
                                {
                                    uniformReadList.Add(variable);
                                }
                            }
                            else
                            {
                                UniformUsedWriteFirstList.Add(variable);
                            }
                        }
                    }
                    else
                    {
                        UniformUsedWriteFirstList.Add(variable);
                    }
                }
                if (assignmentExpression.Operator != AssignmentOperator.Default)
                {
                    if (!UniformReadWriteList.Contains(variable))
                        UniformReadWriteList.Add(variable);
                }
            }

            return assignmentExpression;
        }
    }
}
