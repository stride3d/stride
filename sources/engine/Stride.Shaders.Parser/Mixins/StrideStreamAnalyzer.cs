// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Shaders.Parser.Analysis;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;
using Stride.Core.Shaders.Visitor;

namespace Stride.Shaders.Parser.Mixins
{
    internal class StrideStreamAnalyzer : ShaderWalker
    {
        #region Private members

        /// <summary>
        /// Current stream usage
        /// </summary>
        private StreamUsage currentStreamUsage = StreamUsage.Read;

        /// <summary>
        /// List of stream usage
        /// </summary>
        private List<StreamUsageInfo> currentStreamUsageList = null;

        /// <summary>
        /// List of already added methods.
        /// </summary>
        private List<MethodDeclaration> alreadyAddedMethodsList = null;

        /// <summary>
        /// Status of the assignment
        /// </summary>
        private AssignmentOperatorStatus currentAssignmentOperatorStatus = AssignmentOperatorStatus.Read;

        /// <summary>
        /// Log of all the warnings and errors
        /// </summary>
        private LoggerResult errorWarningLog;

        /// <summary>
        /// Name of the shader
        /// </summary>
        private string shaderName = "Mix";

        #endregion

        #region Public members

        /// <summary>
        /// List of assignations in the form of "streams = ...;"
        /// </summary>
        public Dictionary<AssignmentExpression, StatementList> StreamAssignations = new Dictionary<AssignmentExpression, StatementList>();

        /// <summary>
        /// List of assignations in the form of "... = streams;"
        /// </summary>
        public Dictionary<AssignmentExpression, StatementList> AssignationsToStream = new Dictionary<AssignmentExpression, StatementList>();

        /// <summary>
        /// List of assignations in the form of "StreamType backup = streams;"
        /// </summary>
        public Dictionary<Variable, StatementList> VariableStreamsAssignment = new Dictionary<Variable, StatementList>();

        /// <summary>
        /// streams usage by method
        /// </summary>
        public Dictionary<MethodDeclaration, List<StreamUsageInfo>> StreamsUsageByMethodDefinition = new Dictionary<MethodDeclaration, List<StreamUsageInfo>>();

        /// <summary>
        /// A list containing all the "streams" Variable references
        /// </summary>
        public HashSet<MethodInvocationExpression> AppendMethodCalls = new HashSet<MethodInvocationExpression>();

        #endregion

        #region Constructor

        public StrideStreamAnalyzer(LoggerResult errorLog)
            : base(false, true)
        {
            errorWarningLog = errorLog ?? new LoggerResult();
        }

        #endregion

        public void Run(ShaderClassType shaderClassType)
        {
            shaderName = shaderClassType.Name.Text;
            Visit(shaderClassType);
        }

        #region Private methods

        /// <summary>
        /// Analyse the method definition and store it in the correct lists (based on storage and stream usage)
        /// </summary>
        /// <param name="methodDefinition">the MethodDefinition</param>
        public override void Visit(MethodDefinition methodDefinition)
        {
            currentStreamUsageList = new List<StreamUsageInfo>();
            alreadyAddedMethodsList = new List<MethodDeclaration>();
            
            base.Visit(methodDefinition);

            if (currentStreamUsageList.Count > 0)
                StreamsUsageByMethodDefinition.Add(methodDefinition, currentStreamUsageList);
        }

        /// <summary>
        /// Calls the base method but modify the stream usage beforehand
        /// </summary>
        /// <param name="expression">the method expression</param>
        public override void Visit(MethodInvocationExpression expression)
        {
            base.Visit(expression);

            var methodDecl = expression.Target.TypeInference.Declaration as MethodDeclaration;
            
            if (methodDecl != null)
            {
                // Stream analysis
                if (methodDecl.ContainsTag(StrideTags.ShaderScope)) // this will prevent built-in function to appear in the list
                {
                    // test if the method was previously added
                    if (!alreadyAddedMethodsList.Contains(methodDecl))
                    {
                        currentStreamUsageList.Add(new StreamUsageInfo { CallType = StreamCallType.Method, MethodDeclaration = methodDecl, Expression = expression });
                        alreadyAddedMethodsList.Add(methodDecl);
                    }
                }
                for (int i = 0; i < expression.Arguments.Count; ++i)
                {
                    var arg = expression.Arguments[i] as MemberReferenceExpression; // TODO:

                    if (arg != null && IsStreamMember(arg))
                    {
                        var isOut = methodDecl.Parameters[i].Qualifiers.Contains(Stride.Core.Shaders.Ast.ParameterQualifier.Out);

                        //if (methodDecl.Parameters[i].Qualifiers.Contains(Ast.ParameterQualifier.InOut))
                        //    Error(MessageCode.ErrorInOutStream, expression.Span, arg, methodDecl, contextModuleMixin.MixinName);

                        var usage = methodDecl.Parameters[i].Qualifiers.Contains(Stride.Core.Shaders.Ast.ParameterQualifier.Out) ? StreamUsage.Write : StreamUsage.Read;
                        AddStreamUsage(arg.TypeInference.Declaration as Variable, arg, usage);
                    }
                }
            }

            // TODO: <shaderclasstype>.Append should be avoided
            if (expression.Target is MemberReferenceExpression && (expression.Target as MemberReferenceExpression).Target.TypeInference.TargetType is ClassType && (expression.Target as MemberReferenceExpression).Member.Text == "Append")
                AppendMethodCalls.Add(expression);
        }

        private static bool IsStreamMember(MemberReferenceExpression expression)
        {
            if (expression.TypeInference.Declaration is Variable)
            {
                return (expression.TypeInference.Declaration as Variable).Qualifiers.Contains(StrideStorageQualifier.Stream);
            }
            return false;
        }

        /// <summary>
        /// Analyse the VariableReferenceExpression, detects streams, propagate type inference, get stored in the correct list for later analysis
        /// </summary>
        /// <param name="variableReferenceExpression">the VariableReferenceExpression</param>
        public override void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            base.Visit(variableReferenceExpression);
            // HACK: force types on base, this and stream keyword to eliminate errors in the log an use the standard type inference
            if (variableReferenceExpression.Name == StreamsType.ThisStreams.Name)
            {
                if (!(ParentNode is MemberReferenceExpression)) // streams is alone
                    currentStreamUsageList.Add(new StreamUsageInfo { CallType = StreamCallType.Direct, Variable = StreamsType.ThisStreams, Expression = variableReferenceExpression, Usage = currentStreamUsage });
            }
        }

        public override void Visit(MemberReferenceExpression memberReferenceExpression)
        {
            var usageCopy = currentStreamUsage;
            currentStreamUsage |= StreamUsage.Partial;
            base.Visit(memberReferenceExpression);
            currentStreamUsage = usageCopy;

            // check if it is a stream
            if (IsStreamMember(memberReferenceExpression))
                AddStreamUsage(memberReferenceExpression.TypeInference.Declaration as Variable, memberReferenceExpression, currentStreamUsage);
        }

        public override void Visit(BinaryExpression expression)
        {
            var prevStreamUsage = currentStreamUsage;
            currentStreamUsage = StreamUsage.Read;
            base.Visit(expression);
            currentStreamUsage = prevStreamUsage;
        }

        public override void Visit(UnaryExpression expression)
        {
            var prevStreamUsage = currentStreamUsage;
            currentStreamUsage = StreamUsage.Read;
            base.Visit(expression);
            currentStreamUsage = prevStreamUsage;
        }

        /// <summary>
        /// Analyse the AssignmentExpression to correctly infer the potential stream usage
        /// </summary>
        /// <param name="assignmentExpression">the AssignmentExpression</param>
        public override void Visit(AssignmentExpression assignmentExpression)
        {
            if (currentAssignmentOperatorStatus != AssignmentOperatorStatus.Read)
                errorWarningLog.Error(StrideMessageCode.ErrorNestedAssignment, assignmentExpression.Span, assignmentExpression, shaderName);

            var prevStreamUsage = currentStreamUsage;
            currentStreamUsage = StreamUsage.Read;
            assignmentExpression.Value = (Expression)VisitDynamic(assignmentExpression.Value);
            currentAssignmentOperatorStatus = (assignmentExpression.Operator != AssignmentOperator.Default) ? AssignmentOperatorStatus.ReadWrite : AssignmentOperatorStatus.Write;

            currentStreamUsage = StreamUsage.Write;
            assignmentExpression.Target = (Expression)VisitDynamic(assignmentExpression.Target);

            currentAssignmentOperatorStatus = AssignmentOperatorStatus.Read;
            currentStreamUsage = prevStreamUsage;

            var parentBlock = this.NodeStack.OfType<StatementList>().LastOrDefault();

            if (assignmentExpression.Operator == AssignmentOperator.Default && parentBlock != null)
            {
                if (assignmentExpression.Target is VariableReferenceExpression && (assignmentExpression.Target as VariableReferenceExpression).Name == StreamsType.ThisStreams.Name) // "streams = ...;"
                    StreamAssignations.Add(assignmentExpression, parentBlock);
                else if (assignmentExpression.Value is VariableReferenceExpression && (assignmentExpression.Value as VariableReferenceExpression).Name == StreamsType.ThisStreams.Name) // "... = streams;"
                    AssignationsToStream.Add(assignmentExpression, parentBlock);
            }
        }

        public override void Visit(Variable variableStatement)
        {
            base.Visit(variableStatement);

            var parentBlock = this.NodeStack.OfType<StatementList>().LastOrDefault();
            if (parentBlock != null && variableStatement.Type == StreamsType.Streams && variableStatement.InitialValue is VariableReferenceExpression && ((VariableReferenceExpression)(variableStatement.InitialValue)).TypeInference.TargetType.IsStreamsType())
            {
                VariableStreamsAssignment.Add(variableStatement, parentBlock);
            }
        }

        /// <summary>
        /// Adds a stream usage to the current method
        /// </summary>
        /// <param name="variable">the stream Variable</param>
        /// <param name="expression">the calling expression</param>
        /// <param name="usage">the encountered usage</param>
        private void AddStreamUsage(Variable variable, Stride.Core.Shaders.Ast.Expression expression, StreamUsage usage)
        {
            currentStreamUsageList.Add(new StreamUsageInfo { CallType = StreamCallType.Member, Variable = variable, Expression = expression, Usage = usage });
        }

        #endregion
    }

    [Flags]
    internal enum StreamUsage
    {
        Unknown = 0,
        Read = 1,
        Write = 2,

        /// <summary>
        /// Not all the components of the variable have been read/written
        /// </summary>
        Partial = 4,
    }

    internal static class StreamUsageExtensions
    {
        public static bool IsRead(this StreamUsage usage) { return (usage & StreamUsage.Read) != 0; }
        public static bool IsWrite(this StreamUsage usage) { return (usage & StreamUsage.Write) != 0; }
        public static bool IsPartial(this StreamUsage usage) { return (usage & StreamUsage.Partial) != 0; }
    }

    internal enum StreamCallType
    {
        Unknown = 0,
        Member = 1,
        Method = 2,
        Direct = 3
    }

    internal class StreamUsageInfo
    {
        public StreamUsage Usage = StreamUsage.Unknown;
        public StreamCallType CallType = StreamCallType.Unknown;
        public Variable Variable = null;
        public MethodDeclaration MethodDeclaration = null;
        public Expression Expression;
    }
}
