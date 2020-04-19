// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Stride.Assets.Scripts
{
    public class ConditionalBranchBlock : ExecutionBlock
    {
        [DataMemberIgnore]
        public Slot TrueSlot => FindSlot(TrueSlotDefinition);

        [DataMemberIgnore]
        public Slot FalseSlot => FindSlot(FalseSlotDefinition);

        [DataMemberIgnore]
        public Slot ConditionSlot => FindSlot(ConditionSlotDefinition);

        public static readonly SlotDefinition ConditionSlotDefinition = SlotDefinition.NewValueInput("Condition", "bool", "true");
        public static readonly SlotDefinition TrueSlotDefinition = SlotDefinition.NewExecutionOutput("True");
        public static readonly SlotDefinition FalseSlotDefinition = SlotDefinition.NewExecutionOutput("False", SlotFlags.AutoflowExecution);

        public override string Title => "Condition";

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            newSlots.Add(InputExecutionSlotDefinition);
            newSlots.Add(ConditionSlotDefinition);
            newSlots.Add(TrueSlotDefinition);
            newSlots.Add(FalseSlotDefinition);
        }

        public override void GenerateCode(VisualScriptCompilerContext context)
        {
            // Generate false then true block (false block will be reached if the previous condition failed), then true block (reached by goto)
            var falseBlock = context.GetOrCreateBasicBlockFromSlot(FalseSlot);
            var trueBlock = context.GetOrCreateBasicBlockFromSlot(TrueSlot);

            // Generate condition
            var condition = context.GenerateExpression(ConditionSlot);

            // if (condition) goto trueBlock;
            if (trueBlock != null)
                context.AddStatement(IfStatement(condition, context.CreateGotoStatement(trueBlock)));

            // Execution continue in false block
            context.CurrentBasicBlock.NextBlock = falseBlock;
        }
    }
}
