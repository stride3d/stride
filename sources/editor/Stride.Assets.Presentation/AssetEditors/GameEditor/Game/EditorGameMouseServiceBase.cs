// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Editor.EditorGame.Game;
using Stride.Engine.InputInteractions;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    /// <summary>
    /// Base class for editor game service that can take control of the mouse.
    /// </summary>
    public abstract class EditorGameMouseServiceBase : EditorGameServiceBase, IEditorGameMouseService
    {
        private readonly List<IEditorGameMouseService> mouseServices = new List<IEditorGameMouseService>();

        public IInputInteractionService InteractionService { get; private set; }

        /// <inheritdoc/>
        [Obsolete("Use !InteractionService.HasActiveInteraction")]
        public abstract bool IsControllingMouse { get; protected set; }

        /// <summary>
        /// Gets or sets whether the material selection mode is currently active.
        /// </summary>
        public override bool IsActive { get; set; } = true;

        /// <inheritdoc/>
        [Obsolete("Check with (!InteractionService.HasActiveInteraction || InteractionService.IsActiveInteractionOwner(this))")]
        public bool IsMouseAvailable => !InteractionService.HasActiveInteraction || InteractionService.IsActiveInteractionOwner(this);

        internal void RegisterMouseServices([NotNull] EditorGameServiceRegistry serviceRegistry)
        {
            foreach (var service in serviceRegistry.Services.OfType<IEditorGameMouseService>())
            {
                mouseServices.Add(service);
            }
        }

        internal void InitializeMouseService(IInputInteractionService interactionService)
        {
            InteractionService = interactionService;
        }
    }
}
