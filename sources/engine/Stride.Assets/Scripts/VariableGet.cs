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
    public class VariableGet : ExpressionBlock
    {
        [DefaultValue(""), RegenerateTitle, BlockDropTarget, ScriptVariableReference]
        public string Name { get; set; } = string.Empty;

        public override string Title => Name != null ? $"Get {Name}" : "Get";

        [DataMemberIgnore]
        public Slot ValueSlot => FindSlot(SlotDirection.Output, SlotKind.Value, null);

        public override ExpressionSyntax GenerateExpression(VisualScriptCompilerContext context)
        {
            if (Name == null)
                return IdentifierName("variable_not_set");

            return IdentifierName(Name);
        }

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            newSlots.Add(new Slot(SlotDirection.Output, SlotKind.Value));
        }
    }
}
