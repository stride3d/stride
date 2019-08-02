// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Diagnostics;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.AssetEditors;
using Xenko.Assets.Scripts;

namespace Xenko.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for a <see cref="Method"/> inside a <see cref="VisualScriptAsset"/>.
    /// </summary>
    public class VisualScriptMethodViewModel : DispatcherViewModel
    {
        private readonly VisualScriptViewModel visualScript;
        private readonly Method method;
        private readonly IObjectNode blocksContent;
        private readonly IObjectNode linksContent;
        private readonly IObjectNode parametersContent;
        private readonly MemberGraphNodeBinding<Accessibility> accessibilityNodeBinding;
        private readonly MemberGraphNodeBinding<VirtualModifier> virtualModifierNodeBinding;
        private readonly MemberGraphNodeBinding<bool> isStaticNodeBinding;
        private readonly MemberGraphNodeBinding<string> nameNodeBinding;
        private readonly MemberGraphNodeBinding<string> returnTypeNodeBinding;

        public VisualScriptMethodViewModel(VisualScriptViewModel visualScript, Method method) : base(visualScript.ServiceProvider)
        {
            this.visualScript = visualScript;
            this.method = method;
            var methodNode = visualScript.Session.AssetNodeContainer.GetOrCreateNode(method);
            blocksContent = methodNode[nameof(method.Blocks)].Target;
            linksContent = methodNode[nameof(method.Links)].Target;
            parametersContent = methodNode[nameof(method.Parameters)].Target;

            // Create bindings
            accessibilityNodeBinding = new MemberGraphNodeBinding<Accessibility>(methodNode[nameof(method.Accessibility)], nameof(Accessibility), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
            virtualModifierNodeBinding = new MemberGraphNodeBinding<VirtualModifier>(methodNode[nameof(method.VirtualModifier)], nameof(VirtualModifier), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
            isStaticNodeBinding = new MemberGraphNodeBinding<bool>(methodNode[nameof(method.IsStatic)], nameof(IsStatic), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
            nameNodeBinding = new MemberGraphNodeBinding<string>(methodNode[nameof(method.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
            returnTypeNodeBinding = new MemberGraphNodeBinding<string>(methodNode[nameof(method.ReturnType)], nameof(ReturnType), OnPropertyChanging, OnPropertyChanged, visualScript.UndoRedoService);
        }

        public override void Destroy()
        {
            accessibilityNodeBinding.Dispose();
            virtualModifierNodeBinding.Dispose();
            isStaticNodeBinding.Dispose();
            nameNodeBinding.Dispose();
            returnTypeNodeBinding.Dispose();

            base.Destroy();
        }

        public Accessibility Accessibility { get { return accessibilityNodeBinding.Value; } set { accessibilityNodeBinding.Value = value; } }

        public VirtualModifier VirtualModifier { get { return virtualModifierNodeBinding.Value; } set { virtualModifierNodeBinding.Value = value; } }

        public bool IsStatic { get { return isStaticNodeBinding.Value; } set { isStaticNodeBinding.Value = value; } }

        public string Name { get { return nameNodeBinding.Value; } set { nameNodeBinding.Value = value; } }

        public string ReturnType { get { return returnTypeNodeBinding.Value; } set { returnTypeNodeBinding.Value = value; } }

        public Method Method => method;

        public void AddParameter(Parameter parameter)
        {
            parametersContent.Add(parameter);
        }

        public void RemoveParameter(Parameter parameter)
        {
            // Find index
            var index = method.Parameters.IndexOf(parameter);
            if (index < 0)
                return;

            // TODO: Cleanup references to this parameter

            // Remove
            var itemIndex = new NodeIndex(index);
            parametersContent.Remove(parameter, itemIndex);
        }

        public void AddBlock(Block block)
        {
            blocksContent.Add(block);
        }

        public void RemoveBlock(Block block)
        {
            // First, remove all links this block uses
            var i = 0;
            foreach (var link in method.Links.Values)
            {
                if (link.Source.Owner == block || link.Target.Owner == block)
                {
                    var linkItemIndex = new NodeIndex(i);
                    linksContent.Remove(link, linkItemIndex);

                    // Since we removed an item, fix index of next check
                    --i;
                }
            }

            // Remove
            var itemIndex = new NodeIndex(block.Id);
            blocksContent.Remove(block, itemIndex);
        }

        public void AddLink(Link link)
        {
            linksContent.Add(link);
        }

        public void RemoveLink(Link link)
        {
            // Remove
            var itemIndex = new NodeIndex(link.Id);
            linksContent.Remove(link, itemIndex);
        }

        public async Task RegenerateSlots()
        {
            foreach (var block in method.Blocks.Values)
            {
                await RegenerateSlots(block);
            }
        }

        /// <summary>
        /// Regenerates slot for a given <see cref="Block"/>.
        /// </summary>
        /// <param name="block">The block which slots should be regenerated. It must be part of the asset.</param>
        /// <returns></returns>
        public async Task RegenerateSlots(Block block, ILogger log = null)
        {
            var newSlots = new List<Slot>(block.Slots.Count);
            var sourceResolver = ServiceProvider.Get<IScriptSourceCodeResolver>();

            try
            {
                // Regenerate new list of slots (asynchronously)
                await Task.Run(() => block.GenerateSlots(newSlots, new SlotGeneratorContext(sourceResolver.LatestCompilation)));

                // Update list of slots (try to preserve them if possible)
                UpdateSlots(block, newSlots);
            }
            catch (Exception ex)
            {
                // Allow errors while building slots
                // TODO: Should go to some kind of log in the view model
                log?.Error($"Could not regenerate slots for block [{block}]", ex);
            }
        }

        private void UpdateSlots(Block block, List<Slot> newSlots)
        {
            // Try to remap slots
            var slotMapping = new Dictionary<Slot, Slot>();
            foreach (var slot in block.Slots)
            {
                slotMapping.Add(slot, null);
            }

            bool wasChanged = newSlots.Count != block.Slots.Count;

            for (int i = 0; i < newSlots.Count; i++)
            {
                var newSlot = newSlots[i];

                // Try to find a matching slot in the old list
                int matchingOldSlotIndex = -1;
                Slot matchingOldSlot = null;
                for (int j = 0; j < block.Slots.Count; j++)
                {
                    var oldSlot = block.Slots[j];

                    // We detect similar slot with kind, direction and name
                    // We ignore other attributes such as value, type, flags, etc...
                    if (oldSlot.Kind == newSlot.Kind
                        && oldSlot.Direction == newSlot.Direction
                        && oldSlot.Name == newSlot.Name)
                    {
                        matchingOldSlot = oldSlot;
                        matchingOldSlotIndex = j;
                        break;
                    }
                }

                if (matchingOldSlot != null)
                {
                    // Keep Id stable
                    newSlot.Id = matchingOldSlot.Id;

                    // If there was a value before, keep it
                    if (matchingOldSlot.Value != null)
                        newSlot.Value = matchingOldSlot.Value;

                    // If a type has been set, new one is probably better; otherwise keep old one
                    if (newSlot.Type == null)
                        newSlot.Type = matchingOldSlot.Type;

                    // Check if there was any change
                    if (newSlot.Value != matchingOldSlot.Value || newSlot.Type != matchingOldSlot.Type || newSlot.Flags != matchingOldSlot.Flags)
                    {
                        wasChanged = true;
                        slotMapping[matchingOldSlot] = newSlot;
                    }
                    else
                    {
                        // Keep old slot
                        newSlots[i] = matchingOldSlot;
                        slotMapping[matchingOldSlot] = matchingOldSlot; // Force links to still be removed/added again to trigger view model update
                    }
                }
                else
                {
                    wasChanged = true;
                }

                // Slot moved or disappeared?
                if (matchingOldSlotIndex != i)
                {
                    wasChanged = true;
                }
            }

            // Early exit if nothing changed
            if (!wasChanged)
            {
                return;
            }

            var actionService = ServiceProvider.Get<IUndoRedoService>();
            using (var transaction = actionService.CreateTransaction())
            {
                // Update slot list
                var blockNode = visualScript.Session.AssetNodeContainer.GetOrCreateNode(block);
                var blockSlots = blockNode[nameof(Block.Slots)].Target;

                // Remove all links used by previous slots
                // Note: we completetly remove them before updating, since we regenerates new VisualScriptSlotViewModel, so we need updated VisualScriptLinkViewModel
                var linksToAdd = new List<Link>();
                foreach (var link in method.Links.Values.ToArray())
                {
                    Slot newSlot;
                    var linkRemoved = false;

                    // Source
                    if (slotMapping.TryGetValue(link.Source, out newSlot))
                    {
                        RemoveLink(link);
                        linkRemoved = true;

                        // If slot doesn't exist anymore, we're done
                        if (newSlot == null)
                        {
                            continue;
                        }

                        // Otherwise, update
                        link.Source = newSlot;
                    }

                    // Target
                    if (slotMapping.TryGetValue(link.Target, out newSlot))
                    {
                        RemoveLink(link);
                        linkRemoved = true;

                        // If slot doesn't exist anymore, we're done
                        if (newSlot == null)
                        {
                            continue;
                        }

                        // Otherwise, update
                        link.Target = newSlot;
                    }

                    if (linkRemoved)
                    {
                        // Readd link later
                        linksToAdd.Add(link);
                    }
                }

                // Remove all slots
                // TODO: we could use diff to minimize changes, but probably not worth the effort
                // Note that we can't simply overwrite slots, since the Slot.Owner would become invalid if added before it is removed at another index
                for (int i = block.Slots.Count - 1; i >= 0; --i)
                    blockSlots.Remove(block.Slots[i], new NodeIndex(i));

                // Add new slots (and try to reduce changes)
                foreach (var slot in newSlots)
                {
                    blockSlots.Add(slot);
                }

                // Readd updated links
                foreach (var link in linksToAdd)
                {
                    AddLink(link);
                }

                actionService.SetName(transaction, "Updated slots");
            }
        }
    }
}
