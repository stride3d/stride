// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Utility;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Analysis
{
    /// <summary>
    /// A Type reference analysis is building type references.
    /// </summary>
    public class SemanticAnalysis : AnalysisBase
    {
        protected static string TagBuiltinUserDefined = "TagBuiltinUserDefined";

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticAnalysis"/> class.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        public SemanticAnalysis(ParsingResult result)
            : base(result)
        {
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether the analysis is in skipping mode.
        /// </summary>
        /// <value>
        /// <c>true</c> if the analysis is in skipping mode; otherwise, <c>false</c>.
        /// </value>
        protected bool IsSkippingMode { get; set; }

        #region Public Methods

        /// <inheritdoc/>
        public override void Run()
        {
            Visit(ParsingResult.Shader);
        }

        /// <summary>
        /// Visits the specified assignement expression.
        /// </summary>
        /// <param name="assignmentExpression">The assignement expression.</param>
        public override Node Visit(AssignmentExpression assignmentExpression)
        {
            base.Visit(assignmentExpression);

            // An assignment expression get the
            assignmentExpression.TypeInference = (TypeInference)assignmentExpression.Target.TypeInference.Clone();

            // TODO: check if types are compatible?
            return assignmentExpression;
        }

        /// <summary>
        /// Visits the specified binary expression.
        /// </summary>
        /// <param name="binaryExpression">The binary expression.</param>
        public override Node Visit(BinaryExpression binaryExpression)
        {
            base.Visit(binaryExpression);

            var leftType = binaryExpression.Left.TypeInference.TargetType;
            var rightType = binaryExpression.Right.TypeInference.TargetType;

            // No need to log an error as it has been done by the initial base.Visit(
            if (leftType == null || rightType == null) return binaryExpression;

            switch (binaryExpression.Operator)
            {
                case BinaryOperator.Multiply:
                    binaryExpression.TypeInference.TargetType = GetMultiplyImplicitConversionType(binaryExpression.Span, leftType, rightType);
                    break;
                case BinaryOperator.Divide:
                    binaryExpression.TypeInference.TargetType = GetDivideImplicitConversionType(binaryExpression.Span, leftType, rightType);
                    break;
                case BinaryOperator.Minus:
                case BinaryOperator.Plus:
                case BinaryOperator.Modulo:
                case BinaryOperator.LogicalAnd:
                case BinaryOperator.LogicalOr:
                case BinaryOperator.BitwiseOr:
                case BinaryOperator.BitwiseAnd:
                case BinaryOperator.BitwiseXor:
                case BinaryOperator.RightShift:
                case BinaryOperator.LeftShift:
                    binaryExpression.TypeInference.TargetType = GetBinaryImplicitConversionType(binaryExpression.Span, leftType, rightType, false);
                    break;
                case BinaryOperator.Less:
                case BinaryOperator.LessEqual:
                case BinaryOperator.Greater:
                case BinaryOperator.GreaterEqual:
                case BinaryOperator.Equality:
                case BinaryOperator.Inequality:
                    var returnType = GetBinaryImplicitConversionType(binaryExpression.Span, leftType, rightType, true);
                    binaryExpression.TypeInference.TargetType = TypeBase.CreateWithBaseType(returnType, ScalarType.Bool);
                    break;
            }

            return binaryExpression;
        }

        /// <summary>
        /// Visits the specified conditional expression.
        /// </summary>
        /// <param name="conditionalExpression">The conditional expression.</param>
        public override Node Visit(ConditionalExpression conditionalExpression)
        {
            base.Visit(conditionalExpression);

            // Type inference for conditional expression is using the left result
            var leftType = conditionalExpression.Left.TypeInference.TargetType;
            var rightType = conditionalExpression.Right.TypeInference.TargetType;

            conditionalExpression.TypeInference.TargetType = leftType;
            
            if (leftType == null || (leftType is ScalarType && !(rightType is ScalarType)))
            {
                conditionalExpression.TypeInference.TargetType = rightType;
            }

            return conditionalExpression;
        }

        /// <summary>
        /// Visits the specified indexer expression.
        /// </summary>
        /// <param name="indexerExpression">The indexer expression.</param>
        public override Node Visit(IndexerExpression indexerExpression)
        {
            base.Visit(indexerExpression);

            ProcessIndexerExpression(indexerExpression);

            return indexerExpression;
        }

        /// <summary>
        /// Find the type of the expression
        /// </summary>
        /// <param name="indexerExpression">the indexer expression</param>
        public virtual void ProcessIndexerExpression(IndexerExpression indexerExpression)
        {
            TypeBase type = null;
            var targetType = indexerExpression.Target.TypeInference.TargetType;

            if (targetType is ArrayType)
            {
                var arrayType = (ArrayType)targetType;
                if (arrayType.Dimensions.Count == 1)
                {
                    type = arrayType.Type.ResolveType();
                }
                else
                {
                    var dimensions = new List<Expression>(arrayType.Dimensions);
                    dimensions.RemoveAt(0);
                    type = new ArrayType(arrayType.Type, dimensions.ToArray());
                }
            }
            else if (targetType is VectorType)
            {
                type = ((VectorType)targetType).Type.ResolveType();
            }
            else if (targetType is MatrixType)
            {
                type = new VectorType((ScalarType)((MatrixType)targetType).Type.ResolveType(), ((MatrixType)targetType).ColumnCount);
            }
            else if (targetType is ClassType)
            {
                // This is for buffers<type>, especially in compute shaders
                // TODO: check all the cases
                var classType = (ClassType)targetType;
                if (classType.GenericArguments.Count > 0)
                    type = classType.GenericArguments[0];
            }

            indexerExpression.TypeInference.TargetType = type;
            if (type == null)
                Error(MessageCode.ErrorIndexerType, indexerExpression.Span, indexerExpression);
        }

        /// <summary>
        /// Visits the specified literal expression.
        /// </summary>
        /// <param name="literalExpression">The literal expression.</param>
        public override Node Visit(LiteralExpression literalExpression)
        {
            base.Visit(literalExpression);

            if (literalExpression.Value is int)
                literalExpression.TypeInference.TargetType = ScalarType.Int;
            if (literalExpression.Value is uint)
                literalExpression.TypeInference.TargetType = ScalarType.UInt;
            if (literalExpression.Value is float)
                literalExpression.TypeInference.TargetType = ScalarType.Float;
            if (literalExpression.Value is double)
                literalExpression.TypeInference.TargetType = ScalarType.Double;
            if (literalExpression.Value is bool)
                literalExpression.TypeInference.TargetType = ScalarType.Bool;
            if (literalExpression.Value is string)
                literalExpression.TypeInference.TargetType = TypeBase.String;

            if (literalExpression.TypeInference.TargetType == null)
            {
                Error(MessageCode.ErrorLiteralType, literalExpression.Span, literalExpression.Text);
            }

            return literalExpression;
        }

        public override Node Visit(ReturnStatement returnStatement)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(returnStatement);

            if (returnStatement.Value != null)
            {
                var function = NodeStack.OfType<MethodDefinition>().Last();
                returnStatement.Value.TypeInference.ExpectedType = function.ReturnType.ResolveType();
            }

            return returnStatement;
        }

        public override Node Visit(IfStatement ifStatement)
        {
            // First, dispatch to resolve type of node at deeper level
            base.Visit(ifStatement);

            ifStatement.Condition.TypeInference.ExpectedType = ScalarType.Bool;

            return ifStatement;
        }

        /// <summary>
        /// Pres the process method invocation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="declarations">The declarations.</param>
        /// <returns></returns>
        protected virtual void ProcessMethodInvocation(MethodInvocationExpression expression, string methodName, List<IDeclaration> declarations)
        {
            // Get all arguments types infered
            var argumentTypeInferences = expression.Arguments.Select(x => x.TypeInference).ToArray();
            var argumentTypes = expression.Arguments.Select(x => x.TypeInference.TargetType).ToArray();

            // If any type could not be resolved previously, there is already an error, so return immediately
            if (argumentTypes.Any(x => x == null))
                return;

            var overloads = new List<FunctionOverloadScore>();

            // Use the most overriden method
            // TODO: Temporary workaround for user methods overriding builtin methods
            // Remove the builtin methods if there is any overriding
            var methodsDeclared = declarations.OfType<MethodDeclaration>().ToList();
            for (int i = 0; i < methodsDeclared.Count - 1; i++)
            {
                var leftMethod = methodsDeclared[i];
                for (int j = i + 1; j < methodsDeclared.Count; j++)
                {
                    if (leftMethod.IsSameSignature(methodsDeclared[j]))
                    {
                        methodsDeclared.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            // Try to match the function arguments with every overload
            foreach (var methodDeclaration in methodsDeclared)
            {
                var returnType = methodDeclaration.ReturnType.ResolveType();
                var parameterTypes = methodDeclaration.Parameters.Select(x => x.Type.ResolveType()).ToArray();

                // Number of parameters doesn't match
                if (argumentTypes.Length > parameterTypes.Length) continue;

                // Check for method calls that is using implicit parameter value
                if (argumentTypes.Length < parameterTypes.Length)
                {
                    bool allRemainingParametersHaveDefaultValues = true;
                    // Check for default values
                    for (int i = argumentTypes.Length; i < parameterTypes.Length; i++)
                    {
                        if (methodDeclaration.Parameters[i].InitialValue == null)
                        {
                            allRemainingParametersHaveDefaultValues = false;
                            break;
                        }
                    }

                    // If remaining parameters doesn't have any default values, then continue
                    if (!allRemainingParametersHaveDefaultValues) continue;
                }

                // Higher score = more conversion (score == 0 is perfect match)
                int score = 0;
                bool validOverload = true;

                // Check parameters
                for (int i = 0; i < argumentTypes.Length && validOverload; ++i)
                {
                    var argType = argumentTypes[i];
                    var expectedType = parameterTypes[i];

                    var argTypeBase = TypeBase.GetBaseType(argType);
                    var expectedTypeBase = TypeBase.GetBaseType(expectedType);

                    if (expectedTypeBase is GenericParameterType)
                    {
                        var genericParameterType = (GenericParameterType)expectedTypeBase;

                        // TODO handle dynamic score from constraint.
                        if (methodDeclaration.CheckConstraint(genericParameterType, argType))
                        {
                            score++;
                        }
                        else
                        {
                            validOverload = false;
                        }
                    }
                    else
                    {
                        // TODO, improve the whole test by using TypeBase equality when possible

                        // Then work on scalar type conversion ( float to int, signed to unsigned, different type)
                        var fromScalarType = argTypeBase as ScalarType;
                        var toScalarType = expectedTypeBase as ScalarType;

                        if (fromScalarType != null && toScalarType != null)
                        {
                            if (ScalarType.IsFloat(fromScalarType) && !ScalarType.IsFloat(toScalarType))
                            {
                                // Truncation from float to int
                                score += 7;
                            }
                            else if (fromScalarType != toScalarType)
                            {
                                // else different type (implicit cast is usually working)
                                score += 1;
                            }

                            if (!fromScalarType.IsUnsigned && toScalarType.IsUnsigned)
                            {
                                // int to unsigned 
                                score += 2;
                            }
                        }

                        // First, try to fix the base type (i.e. the "float" of float3x1)
                        if (argTypeBase != expectedTypeBase && expectedTypeBase is ScalarType)
                        {
                            if (!(argTypeBase is ScalarType))
                            {
                                score++; // +1 for type conversion
                            }
                            argType = TypeBase.CreateWithBaseType(argType, (ScalarType)expectedTypeBase);
                        }

                        validOverload = TestMethodInvocationArgument(argTypeBase, expectedTypeBase, argType, expectedType, ref score);
                    }
                }

                if (validOverload)
                    overloads.Add(
                        new FunctionOverloadScore { Declaration = methodDeclaration, ParameterTypes = parameterTypes, ReturnType = returnType, Score = score });
            }

            // In-place sort using List.Sort would be lighter
            var bestOverload = overloads.OrderBy(x => x.Score).FirstOrDefault();

            if (bestOverload != null)
            {
                expression.TypeInference.TargetType = bestOverload.ReturnType.ResolveType();

                // Override declaration to match exactly the declaration found by method overloaded resolution
                expression.Target.TypeInference.Declaration = bestOverload.Declaration;

                // Add appropriate cast
                for (int i = 0; i < argumentTypes.Length; ++i)
                {
                    argumentTypeInferences[i].ExpectedType = (bestOverload.ParameterTypes[i] is GenericParameterType)
                                                                 ? argumentTypes[i]
                                                                 : bestOverload.ParameterTypes[i].ResolveType();
                }
            }
            else
            {
                Error(MessageCode.ErrorNoOverloadedMethod, expression.Span, methodName);
            }
        }

        /// <summary>
        /// Tests the arguments of the method
        /// </summary>
        /// <param name="argTypeBase">the argument typebase</param>
        /// <param name="expectedTypeBase">the expected typebase</param>
        /// <param name="argType">the argument type</param>
        /// <param name="expectedType">the expected type</param>
        /// <param name="score">the score of the overload</param>
        /// <returns>true if the overload is correct, false otherwise</returns>
        protected virtual bool TestMethodInvocationArgument(TypeBase argTypeBase, TypeBase expectedTypeBase, TypeBase argType, TypeBase expectedType, ref int score)
        {
            var validOverload = true;
            
            // If Scalar, Vector or Matrix, check types
            if (TypeBase.HasDimensions(argType) && TypeBase.HasDimensions(expectedType))
            {
                if (argType != expectedType)
                {
                    int argDim1 = TypeBase.GetDimensionSize(argType, 0);
                    int argDim2 = TypeBase.GetDimensionSize(argType, 1);
                    int expectedDim1 = TypeBase.GetDimensionSize(expectedType, 0);
                    int expectedDim2 = TypeBase.GetDimensionSize(expectedType, 1);

                    // float3<=>float1x3 and float3<=>float3x1 implicit conversion are allowed,
                    // but float3x1<=>float1x3 should not be allowed
                    // float3<=>float1x3 and float1x1<=>float1<=>float
                    if (argDim1 == expectedDim1 && argDim2 == expectedDim2)
                    {
                        score++;
                    }
                    else if (((argType is VectorType && expectedType is MatrixType) || (argType is MatrixType && expectedType is VectorType))
                             && (argDim1 == expectedDim2 && argDim2 == expectedDim1))
                    {
                        // float3<=>float3x1
                        score++;
                    }
                    else if (argDim1 == 1 && argDim2 == 1)
                    {
                        // allow float=>float3x2 and float=>float3
                        score += 10; // +10 for scalar=>vector or scalar=>matrix expansion
                    }
                    else if (argDim1 >= expectedDim1 && argDim2 >= expectedDim2)
                    {
                        // Truncation
                        // +100 for truncation (by rank difference)
                        score += 100 * (argDim1 + argDim2 - expectedDim1 - expectedDim2);
                    }
                    else
                    {
                        // Could not find any matching implicit conversion
                        validOverload = false;
                    }
                }
            }
            else if (argType is ArrayType && expectedType is ArrayType)
            {
                var argArrayType = (ArrayType)argType;
                var expectedArrayType = (ArrayType)expectedType;

                if (argArrayType != expectedArrayType)
                    validOverload = false;
            }
            else if (argType is StructType && expectedType is StructType)
            {
                var argStructType = (StructType)argType;
                var expectedStructType = (StructType)expectedType;

                if (argStructType.Name != expectedStructType.Name)
                    validOverload = false;
            }
            else if (!((argType is ObjectType && expectedType is ObjectType) || (argType is StructType && expectedType is StructType)))
            {
                // Could not find any matching implicit conversion
                validOverload = false;
            }

            return validOverload;
        }

        /// <summary>
        /// Visits the specified method invocation expression.
        /// </summary>
        /// <param name="expression">The method invocation expression.</param>
        public override Node Visit(MethodInvocationExpression expression)
        {
            base.Visit(expression);
            
            var methodAsVariable = expression.Target as VariableReferenceExpression;
            var methodAsType = expression.Target as TypeReferenceExpression;

            IEnumerable<IDeclaration> declarationsIterator = null;

            string methodName;

            // Check if this is a Variable or Typename
            if (methodAsVariable != null)
            {
                methodName = methodAsVariable.Name.Text;
                declarationsIterator = FindDeclarations(methodAsVariable.Name);
            }
            else if (methodAsType != null)
            {
                var returnType = methodAsType.Type.ResolveType();
                expression.TypeInference.TargetType = returnType;

                if (!(returnType is ScalarType || returnType is VectorType || returnType is MatrixType))
                    Warning(MessageCode.WarningTypeAsConstructor, expression.Span, expression.Target);

                return expression;
            }
            else
            {
                var target = expression.Target as MemberReferenceExpression;
                if (target != null)
                {
                    var memberReferenceExpression = target;

                    declarationsIterator = FindDeclarationsFromObject(memberReferenceExpression.Target.TypeInference.TargetType, memberReferenceExpression.Member.Text);
                    methodName = string.Format("{0}", target);
                }
                else
                {
                    Warning(MessageCode.WarningTypeInferenceUnknownExpression, expression.Span, expression.Target);
                    methodName = string.Format("{0}", expression.Target);
                }
            }

            // If no declarations were found, this is an error
            if (declarationsIterator == null)
            {
                Error(MessageCode.ErrorNoReferencedMethod, expression.Span, methodName);
                return expression;
            }

            // Grab the declarations
            var declarations = declarationsIterator.ToList();
            ProcessMethodInvocation(expression, methodName, declarations);

            return expression;
        }

        protected virtual IEnumerable<IDeclaration> FindDeclarationsFromObject(TypeBase typeBase, string memberName)
        {
            return null;
        }

        /// <summary>
        /// Visits the specified member reference.
        /// </summary>
        /// <param name="memberReference">The member reference.</param>
        public override Node Visit(MemberReferenceExpression memberReference)
        {
            base.Visit(memberReference);

            CommonVisit(memberReference);

            // If member reference is used from method invocation expression, let the method invocation resolve the type
            if (!(ParentNode is MethodInvocationExpression) && memberReference.TypeInference.TargetType == null)
                Warning(MessageCode.WarningNoTypeReferenceMember, memberReference.Span, memberReference);

            return memberReference;
        }

        /// <summary>
        /// Visits the specified member reference.
        /// </summary>
        /// <param name="memberReference">The member reference.</param>
        public override Node Visit(MethodDefinition methodDefinition)
        {
            base.Visit(methodDefinition);

            // Check that this method definition doesn't have a method declaration before
            foreach (var declaration in FindDeclarations(methodDefinition.Name))
            {
                var methodDeclaration = declaration as MethodDeclaration;
                if (methodDeclaration != null && !ReferenceEquals(declaration, methodDefinition))
                {
                    if (methodDeclaration.IsSameSignature(methodDefinition))
                    {
                        methodDefinition.Declaration = methodDeclaration;
                        // Remove the definition if the declaration is tagged as builtin special (user defined)
                        if (methodDeclaration.GetTag(TagBuiltinUserDefined) != null)
                            return null;
                        break;
                    }
                }
            }
            return methodDefinition;
        }

        /// <summary>
        /// Visits the specified member reference.
        /// </summary>
        /// <param name="memberReference">The member reference.</param>
        protected virtual void CommonVisit(MemberReferenceExpression memberReference)
        {
            var thisType = memberReference.Target.TypeInference.TargetType;

            if (thisType is StructType)
                FindMemberTypeReference((StructType)thisType, memberReference);
            else if (thisType is ScalarType)
                FindMemberTypeReference((ScalarType)thisType, memberReference);
            else if (thisType is VectorType)
                FindMemberTypeReference((VectorType)thisType, memberReference);
        }

        /// <summary>
        /// Visits the specified parenthesized expression.
        /// </summary>
        /// <param name="parenthesizedExpression">The parenthesized expression.</param>
        public override Node Visit(ParenthesizedExpression parenthesizedExpression)
        {
            base.Visit(parenthesizedExpression);

            // Get the type from the last item
            parenthesizedExpression.TypeInference = (TypeInference)parenthesizedExpression.Content.TypeInference.Clone();
            return parenthesizedExpression;
        }

        /// <summary>
        /// Visits the specified expression list.
        /// </summary>
        /// <param name="expressionList">The expression list.</param>
        public override Node Visit(ExpressionList expressionList)
        {
            base.Visit(expressionList);

            // Get the type from the last item
            expressionList.TypeInference = (TypeInference)expressionList[expressionList.Count - 1].TypeInference.Clone();
            return expressionList;
        }

        /// <summary>
        /// Visits the specified array type.
        /// </summary>
        /// <param name="arrayType">Array type.</param>
        public override Node Visit(ArrayType arrayType)
        {
            base.Visit(arrayType);

            // Process only if there is non-literal expressions
            if (arrayType.TypeInference.TargetType == null
                && arrayType.Dimensions.Any(x => !(x is LiteralExpression || x is EmptyExpression)))
            {
                // Try to evaluate each dimension into a Literal expression (i.e. float4[3 * 2] should become float4[6])
                var evaluator = new ExpressionEvaluator();
                var results = arrayType.Dimensions.Select(evaluator.Evaluate).ToArray();

                if (results.Any(x => x.HasErrors))
                {
                    foreach (var result in results.Where(x => x.HasErrors))
                        result.CopyTo(ParsingResult);
                }
                else
                {
                    arrayType.TypeInference.TargetType = new ArrayType(arrayType.Type, results.Select(x => new LiteralExpression(Convert.ToInt32(x.Value))).ToArray());
                }
            }

            return arrayType;
        }

        /// <summary>
        /// Visits the specified type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public override Node Visit(TypeName typeName)
        {
            base.Visit(typeName);

            if (typeName.TypeInference.TargetType == null)
            {
                if (typeName.Name.Text == "void")
                {
                    typeName.TypeInference.TargetType = TypeBase.Void;
                }
                else if (typeName.Name.Text == "string")
                {
                    typeName.TypeInference.TargetType = TypeBase.String;
                }
                else
                {
                    var declaration = FindDeclaration(typeName.Name);
                    if (declaration != null)
                    {
                        // Setup a type reference for this typeName
                        var typeReference = typeName.TypeInference;
                        typeReference.Declaration = declaration;
                        typeReference.TargetType = ResolveTypeFromDeclaration(typeReference.Declaration);
                    }
                    else
                    {
                        Error(MessageCode.ErrorNoTypeReferenceTypename, typeName.Span, typeName);
                    }
                }
            }
            return typeName;
        }

        /// <summary>
        /// Visits the specified variable reference expression.
        /// </summary>
        /// <param name="variableReferenceExpression">The variable reference expression.</param>
        public override Node Visit(VariableReferenceExpression variableReferenceExpression)
        {
            base.Visit(variableReferenceExpression);

            var typeReference = variableReferenceExpression.TypeInference;
            typeReference.Declaration = FindDeclaration(variableReferenceExpression.Name);
            typeReference.TargetType = ResolveTypeFromDeclaration(typeReference.Declaration);
            return variableReferenceExpression;
        }

        /// <summary>
        /// Visits the specified unary expression.
        /// </summary>
        /// <param name="unaryExpression">The unary expression.</param>
        public override Node Visit(UnaryExpression unaryExpression)
        {
            base.Visit(unaryExpression);

            // TODO check for 
            unaryExpression.TypeInference = (TypeInference)unaryExpression.Expression.TypeInference.Clone();

            // If this is a logical not, transform the value to a bool (bool2 bool3 bool4 / matrix<bool,1,1> matrix<bool,1,2> ..etc.
            var subType = unaryExpression.Expression.TypeInference.TargetType;
            if (subType != null && unaryExpression.Operator == UnaryOperator.LogicalNot)
                unaryExpression.TypeInference.TargetType = TypeBase.CreateWithBaseType(subType, ScalarType.Bool);

            return unaryExpression;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Finds the member type reference.
        /// </summary>
        /// <param name="structType">Type of the struct.</param>
        /// <param name="memberReference">The member reference.</param>
        protected virtual void FindMemberTypeReference(StructType structType, MemberReferenceExpression memberReference)
        {
            foreach (var field in structType.Fields)
            {
                foreach (var variableDeclarator in field.Instances())
                {
                    if (variableDeclarator.Name == memberReference.Member)
                    {
                        memberReference.TypeInference.Declaration = variableDeclarator;
                        memberReference.TypeInference.TargetType = variableDeclarator.Type.ResolveType();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the member type reference.
        /// </summary>
        /// <param name="vectorType">Type of the vector.</param>
        /// <param name="memberReference">The member reference.</param>
        protected virtual void FindMemberTypeReference(VectorType vectorType, MemberReferenceExpression memberReference)
        {
            var scalarType = vectorType.Type.ResolveType();

            var components = memberReference.Member.Text;
            if (components.Length <= 4 && (
                components.All(x => x == 'x' || x == 'y' || x == 'z' || x == 'w') || 
                components.All(x => x == 'r' || x == 'g' || x == 'b' || x == 'a') ||
                components.All(x => x == 's' || x == 't' || x == 'u' || x == 'v')
                ))
            {
                memberReference.TypeInference.TargetType = components.Length == 1 ? scalarType : new VectorType((ScalarType)scalarType, components.Length);
            }
        }

        /// <summary>
        /// Finds the member type reference.
        /// </summary>
        /// <param name="scalarType">Type of the scalar.</param>
        /// <param name="memberReference">The member reference.</param>
        protected virtual void FindMemberTypeReference(ScalarType scalarType, MemberReferenceExpression memberReference)
        {
            var components = memberReference.Member.Text;
            if (components.Length <= 4 && (
                components.All(x => x == 'x' || x == 'y' || x == 'z' || x == 'w') ||
                components.All(x => x == 'r' || x == 'g' || x == 'b' || x == 'a') ||
                components.All(x => x == 's' || x == 't' || x == 'u' || x == 'v')
                ))
            {
                memberReference.TypeInference.TargetType = components.Length == 1 ? (TypeBase)scalarType : new VectorType(scalarType, components.Length);
            }
        }
        
        /// <summary>
        /// Resolves the type from declaration.
        /// </summary>
        /// <param name="declaration">The declaration.</param>
        /// <returns>
        /// A type
        /// </returns>
        protected TypeBase ResolveTypeFromDeclaration(IDeclaration declaration)
        {
            TypeBase type = null;

            if (declaration is Variable)
            {
                var variableDeclaration = (Variable)declaration;
                type = variableDeclaration.Type.ResolveType();
            }
            else if (declaration is TypeBase)
            {
                type = ((TypeBase)declaration).ResolveType();
            }
            else if (declaration is GenericDeclaration)
            {
                var genericDeclaration = (GenericDeclaration)declaration;
                type = genericDeclaration.Holder.GenericParameters[genericDeclaration.Index].ResolveType();
            }

            if (type is TypeName)
            {
                type = ResolveTypeFromDeclaration(type.TypeInference.Declaration);
            }

            return type;
        }

        class FunctionOverloadScore
        {
            public MethodDeclaration Declaration { get; set; }
            public TypeBase ReturnType { get; set; }
            public TypeBase[] ParameterTypes { get; set; }
            public int Score { get; set; }

            public override string ToString()
            {
                return string.Format("#{0} {1}", Score, Declaration);
            }
        }

        #endregion
    }
}
