// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Core;
using Stride.Core.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Stride.Assets.Scripts
{
    public class MethodCallBlock : ExecutionBlock, IExpressionBlock
    {
        [RegenerateTitle]
        public string MethodName { get; set; }

        [RegenerateTitle]
        public bool IsMemberCall { get; set; }

        [DataMemberIgnore]
        public Slot ReturnSlot => FindSlot(SlotDirection.Output, SlotKind.Value, null);

        public override string Title
        {
            get
            {
                // Take up to two qualifiers (class+method)
                var titleStart = MethodName.LastIndexOf('.');
                titleStart = titleStart > 0 ? MethodName.LastIndexOf('.', titleStart - 1) : -1;

                return MethodName.Substring(titleStart + 1);
            }
        }

        public ExpressionSyntax GenerateExpression(VisualScriptCompilerContext context, Slot slot)
        {
            // TODO: Out/ref
            // Other cases should have been handled by context.RegisterLocalVariable during code generation
            // It's also possible that this block is actually not executed and used as input, so we issue a warning anyway
            context.Log.Error($"No value found for slot {slot}. Note that out/ref slots are not implemented yet.", CallerInfo.Get());
            return null;
        }

        public override void GenerateCode(VisualScriptCompilerContext context)
        {
            var arguments = new List<SyntaxNodeOrToken>();
            var memberCallProcessed = false;

            var invocationTarget = MethodName;

            for (int index = 0; index < Slots.Count; index++)
            {
                var slot = Slots[index];
                if (slot.Direction == SlotDirection.Input && slot.Kind == SlotKind.Value)
                {
                    var argument = context.GenerateExpression(slot);

                    if (IsMemberCall && !memberCallProcessed)
                    {
                        // this parameter (non-static or extension method)
                        memberCallProcessed = true;
                        invocationTarget = argument.ToFullString() +  "." + invocationTarget;
                        continue;
                    }

                    if (arguments.Count > 0)
                        arguments.Add(Token(SyntaxKind.CommaToken));

                    arguments.Add(Argument(argument));
                }
            }

            var expression = InvocationExpression(ParseExpression(invocationTarget), ArgumentList(SeparatedList<ArgumentSyntax>(arguments)));
            var statement = (StatementSyntax)ExpressionStatement(expression);

            // Only store return variable if somebody is using it
            if (ReturnSlot != null && context.FindOutputLinks(ReturnSlot).Any())
            {
                var localVariableName = context.GenerateLocalVariableName();

                // Store return value in a local variable
                statement =
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("var"))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                    Identifier(localVariableName))
                                .WithInitializer(
                                    EqualsValueClause(expression)))));

                context.RegisterLocalVariable(ReturnSlot, localVariableName);
            }

            context.AddStatement(statement);
        }

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            newSlots.Add(InputExecutionSlotDefinition);
            newSlots.Add(OutputExecutionSlotDefinition);

            // Keep old slots
            // We regenerate them based on user actions
            for (int index = 2; index < Slots.Count; index++)
            {
                newSlots.Add(Slots[index]);
            }
        }
    }
}
