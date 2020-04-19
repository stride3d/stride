// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.ObjectModel;
using System.Windows.Media;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    /// <summary>
    /// View model for a <see cref="Scripts.Slot"/>.
    /// </summary>
    public class VisualScriptSlotViewModel : DispatcherViewModel
    {
        private readonly Slot slot;
        private readonly IObjectNode slotNode;
        private bool connected;

        public VisualScriptSlotViewModel(VisualScriptBlockViewModel block, Slot slot) : base(block.SafeArgument(nameof(block)).ServiceProvider)
        {
            this.Block = block;
            this.slot = slot;
            this.slotNode = block.Method.Editor.Session.AssetNodeContainer.GetOrCreateNode(slot);
        }

        internal Slot Slot => slot;

        /// <summary>
        /// The block this slot belongs to.
        /// </summary>
        public VisualScriptBlockViewModel Block { get; }

        /// <summary>
        /// Links connected to that slot.
        /// </summary>
        public ObservableCollection<VisualScriptLinkViewModel> Links { get; } = new ObservableCollection<VisualScriptLinkViewModel>();

        public string Name => slot.Name;

        public SlotKind Kind => slot.Kind;

        public bool Connected { get { return connected; } set { SetValue(ref connected, value); } }
    }
}
