// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Core.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Stride.Assets.Scripts
{
    public class CustomCodeBlock : ExecutionBlock
    {
        private const int CodeAsTitleMaxLength = 40;

        [RegenerateTitle]
        public string Name { get; set; }

        [RegenerateSlots, RegenerateTitle, ScriptCode]
        public string Code { get; set; }

        public override string Title => !string.IsNullOrEmpty(Name) ? Name : GetTitleFromCode();

        public override void GenerateCode(VisualScriptCompilerContext context)
        {
            var blockStatement = ParseStatement($"{{ {Code} }}");
            
            // Forward diagnostics to log
            foreach (var diagnostic in blockStatement.GetDiagnostics())
            {
                LogMessageType logType;
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Info:
                        logType = LogMessageType.Info;
                        break;
                    case DiagnosticSeverity.Warning:
                        logType = LogMessageType.Warning;
                        break;
                    case DiagnosticSeverity.Error:
                        logType = LogMessageType.Error;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                context.Log.Log(new LogMessage(nameof(CustomCodeBlock), logType, diagnostic.GetMessage()));
            }

            var block = blockStatement as BlockSyntax;
            if (block != null)
            {
                RoslynHelper.CreateCompilationUnitFromBlock(ref block);

                foreach (var slot in Slots.Where(x => x.Kind == SlotKind.Value))
                {
                    var symbolsToReplace = block.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(x => x.Identifier.Text == slot.Name)
                        .Where(x =>
                        {
                            // We can never be on the right side of a member access
                            var memberAccess = (x.Parent as MemberAccessExpressionSyntax);
                            return memberAccess == null || memberAccess.Expression == x;
                        })
                        .ToArray();

                    if (slot.Direction == SlotDirection.Input)
                    {
                        // Input
                        // Find expression
                        var expression = context.GenerateExpression(slot);
                        block = block.ReplaceNodes(symbolsToReplace, (x1, x2) => expression);
                    }
                    else
                    {
                        // Output

                        // Replace every reference of slot.Name into generated slotName
                        var slotName = context.GenerateLocalVariableName(slot.Name);
                        block = block.ReplaceNodes(symbolsToReplace, (x1, x2) => x1.WithIdentifier(Identifier(slotName)));

                        // Register a local var with generated name
                        context.RegisterLocalVariable(slot, slotName);
                    }
                }

                foreach (var statement in block.Statements)
                    context.AddStatement(statement);
            }
        }

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            newSlots.Add(InputExecutionSlotDefinition);
            newSlots.Add(OutputExecutionSlotDefinition);

            if (context.Compilation != null && !string.IsNullOrEmpty(Code))
            {
                var statement = ParseStatement($"{{ {Code} }}");

                var block = statement as BlockSyntax;
                if (block != null)
                {
                    RoslynHelper.AnalyzeBlockFlow(newSlots, context.Compilation, block);
                }
            }
        }

        private string GetTitleFromCode()
        {
            if (string.IsNullOrEmpty(Code))
                return "Custom Code";

            var firstLineLength = Code.IndexOfAny(new[] { '\r', '\n' });
            if (firstLineLength == -1)
                firstLineLength = Code.Length;

            // Too long?
            if (firstLineLength > CodeAsTitleMaxLength)
                return Code.Substring(0, CodeAsTitleMaxLength) + "…";

            return Code;
        }
    }
}
