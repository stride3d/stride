// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.NodePresenters.Commands
{
    public class SetComponentReferenceCommand : ChangeValueCommandBase
    {
        public struct Parameter
        {
            public EntityViewModel Entity;
            public int Index;
        }

        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "SetComponentReference";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return typeof(EntityComponent).IsAssignableFrom(nodePresenter.Type);
        }

        /// <inheritdoc/>
        protected override object ChangeValue(object currentValue, object parameter, object preExecuteResult)
        {
            var param = (Parameter)parameter;
            return param.Entity?.AssetSideEntity.Components[param.Index];
        }
    }
}
