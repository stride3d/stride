// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Interop;
using Stride.Games;

namespace Stride.Editor.Engine
{
    /// <summary>
    /// A specialization of <see cref="GameForm"/> that is able to forward keyboard and mousewheel events to an associated <see cref="GameEngineHost"/>.
    /// </summary>
    [System.ComponentModel.DesignerCategory("")]
    public class EmbeddedGameForm : GameForm
    {
        public EmbeddedGameForm()
        {
            enableFullscreenToggle = false;
        }

        /// <summary>
        /// Gets or sets the <see cref="GameEngineHost"/> associated to this form.
        /// </summary>
        public GameEngineHost Host { get; set; }

        /// <inheritdoc/>
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (Host != null)
            {
                switch (m.Msg)
                {
                    case NativeHelper.WM_KEYDOWN:
                    case NativeHelper.WM_KEYUP:
                    case NativeHelper.WM_MOUSEWHEEL:
                    case NativeHelper.WM_RBUTTONDOWN:
                    case NativeHelper.WM_RBUTTONUP:
                    case NativeHelper.WM_LBUTTONDOWN:
                    case NativeHelper.WM_LBUTTONUP:
                    case NativeHelper.WM_MOUSEMOVE:
                    case NativeHelper.WM_CONTEXTMENU:
                        Host.ForwardMessage(m.HWnd, m.Msg, m.WParam, m.LParam);
                        break;
                }
            }
            base.WndProc(ref m);
        }
    }
}
