// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Scripts
{
    public class CustomExpression : ExpressionBlock
    {
        public string Name { get; set; }

        [RegenerateSlots, RegenerateTitle]
        public string Expression { get; set; }

        public override string Title => !string.IsNullOrEmpty(Name) ? Name : (!string.IsNullOrEmpty(Expression) ? Expression : "Custom Pure Expression");

        public override ExpressionSyntax GenerateExpression(VisualScriptCompilerContext context)
        {
            var expression = ParseExpression(Expression);

            // Forward diagnostics to log
            foreach (var diagnostic in expression.GetDiagnostics())
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

                context.Log.Log(new LogMessage(nameof(CustomExpression), logType, diagnostic.GetMessage()));
            }

            return expression;
        }

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            if (context.Compilation != null && !string.IsNullOrEmpty(Expression))
            {
                // Parse expression
                var expression = ParseExpression(Expression);
                
                // Create a block that returns this expression for further analysis
                var block = Block(
                    SingletonList<StatementSyntax>(
                        ReturnStatement(ParenthesizedExpression(expression))));

                RoslynHelper.AnalyzeBlockFlow(newSlots, context.Compilation, block);
            }
        }
    }
}
