// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Assets.Scripts
{
    [DataContract(Inherited = true)]
    public abstract class Block : IIdentifiable, IAssetPartDesign<Block>
    {
        protected Block()
        {
            Id = Guid.NewGuid();
            Slots.CollectionChanged += Slots_CollectionChanged;
        }

        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        /// <inheritdoc/>
        [DataMember(-90), Display(Browsable = false)]
        [DefaultValue(null)]
        public BasePart Base { get; set; }

        /// <summary>
        /// Gets or sets the position of the block.
        /// </summary>
        [DataMember(-50), Display(Browsable = false)]
        public Int2 Position { get; set; }

        /// <summary>
        /// Gets the title of that node, as it will be displayed in the editor.
        /// </summary>
        [DataMemberIgnore]
        public virtual string Title => null;

        /// <summary>
        /// Gets the list of slots this block has.
        /// </summary>
        [DataMember(10000), Display(Browsable = false)]
        public TrackingCollection<Slot> Slots { get; } = new TrackingCollection<Slot>();

        /// <summary>
        /// Generates a list of slot. This doesn't change any state in the <see cref="Block"/>.
        /// </summary>
        /// <param name="newSlots">List to which generated slots will be added.</param>
        /// <param name="context">The context that might be used to access additional information.</param>
        public abstract void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context);

        protected Slot FindSlot(SlotDirection direction, SlotKind kind, string name)
        {
            foreach (var slot in Slots)
            {
                if (slot.Direction == direction && slot.Kind == kind && slot.Name == name)
                    return slot;
            }

            return null;
        }

        protected Slot FindSlot(SlotDirection direction, SlotKind kind, SlotFlags flags)
        {
            foreach (var slot in Slots)
            {
                if (slot.Direction == direction && slot.Kind == kind && (slot.Flags & flags) == flags)
                    return slot;
            }

            return null;
        }

        protected Slot FindSlot(SlotDefinition definition)
        {
            foreach (var slot in Slots)
            {
                if (slot.Direction == definition.Direction && slot.Kind == definition.Kind && slot.Name == definition.Name && slot.Flags == definition.Flags)
                    return slot;
            }

            return null;
        }

        /// <inheritdoc/>
        IIdentifiable IAssetPartDesign.Part => this;

        /// <inheritdoc/>
        Block IAssetPartDesign<Block>.Part => this;

        protected virtual void OnSlotAdd(Slot slot)
        {
            slot.Owner = this;
        }

        protected virtual void OnSlotRemove(Slot slot)
        {
            slot.Owner = null;
        }

        private void Slots_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnSlotAdd((Slot)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnSlotRemove((Slot)e.Item);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public override string ToString()
        {
            return Title ?? base.ToString();
        }
    }

    public abstract class ExecutionBlock : Block
    {
        [DataMemberIgnore]
        public virtual Slot InputExecution => FindSlot(InputExecutionSlotDefinition);

        [DataMemberIgnore]
        public virtual Slot OutputExecution => FindSlot(OutputExecutionSlotDefinition);

        public static readonly SlotDefinition InputExecutionSlotDefinition = SlotDefinition.NewExecutionInput(null);
        public static readonly SlotDefinition OutputExecutionSlotDefinition = SlotDefinition.NewExecutionOutput(null, SlotFlags.AutoflowExecution);

        public abstract void GenerateCode(VisualScriptCompilerContext context);
    }

    public interface IExpressionBlock
    {
        ExpressionSyntax GenerateExpression(VisualScriptCompilerContext context, Slot slot);
    }

    public abstract class ExpressionBlock : Block, IExpressionBlock
    {
        ExpressionSyntax IExpressionBlock.GenerateExpression(VisualScriptCompilerContext context, Slot slot)
        {
            return GenerateExpression(context);
        }

        public abstract ExpressionSyntax GenerateExpression(VisualScriptCompilerContext context);
    }

    public class FunctionStartBlock : ExecutionBlock
    {
        public const string StartSlotName = "Start";

        public override string Title => "Start";

        [DataMemberIgnore]
        public override Slot OutputExecution => FindSlot(SlotDirection.Output, SlotKind.Execution, SlotFlags.AutoflowExecution);

        public override void GenerateSlots(IList<Slot> newSlots, SlotGeneratorContext context)
        {
            newSlots.Add(new Slot(SlotDirection.Output, SlotKind.Execution, StartSlotName, SlotFlags.AutoflowExecution));
        }

        public override void GenerateCode(VisualScriptCompilerContext context)
        {
            // Nothing to do (since we have autoflow on Start slot)
        }
    }

    public enum SlotKind
    {
        Value = 0,
        Execution = 1,
    }

    public enum SlotDirection
    {
        Input = 0,
        Output = 1,
    }

    [Flags]
    public enum SlotFlags
    {
        None = 0,
        AutoflowExecution = 1,
    }
}
