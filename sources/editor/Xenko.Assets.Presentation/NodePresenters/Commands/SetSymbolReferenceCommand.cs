// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Assets.Scripts;

namespace Xenko.Assets.Presentation.NodePresenters.Commands
{
    public class SetSymbolReferenceCommand : ChangeValueCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "SetSymbolReference";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            var member = nodePresenter as MemberNodePresenter;
            return nodePresenter.Type == typeof(string) && (member?.MemberAttributes.OfType<ScriptVariableReferenceAttribute>().Any() ?? false);
        }

        /// <inheritdoc/>
        protected override object ChangeValue(object currentValue, object parameter, object preExecuteResult)
        {
            var symbol = (Symbol)parameter;
            return symbol.Name;
        }
    }
}
