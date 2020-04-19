// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Core;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Stride.Assets.Scripts
{
    public class VariableSet : ExecutionBlock
    {
        [DefaultValue(""), RegenerateTitle, BlockDropTarget, ScriptVariableReference]
        public string Name { get; set; } = string.Empty;

        public override string Title => Name != null ? $"Set {Name}" : "Set";

        [DataMemberIgnore]
        public Slot InputSlot => FindSlot(SlotDirection.Input, SlotKind.Value, null);

        //[DataMemberIgnore]
        //public Slot OutputSlot => FindSlot(SlotDirection.Output, SlotKind.Value, null);

        public override void GenerateCode(VisualScriptCompilerContext context)
        {
            if (Name == null)
                return;

            // Evaluate value
            var newValue = context.GenerateExpression(InputSlot);

            // Generate assignment statement
            context.AddStatement(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(Name), newValue)));
        }

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            newSlots.Add(InputExecutionSlotDefinition);
            newSlots.Add(OutputExecutionSlotDefinition);

            // TODO: Guess type (from context.Compilation)?
            newSlots.Add(new Slot(SlotDirection.Input, SlotKind.Value));
        }
    }
}
