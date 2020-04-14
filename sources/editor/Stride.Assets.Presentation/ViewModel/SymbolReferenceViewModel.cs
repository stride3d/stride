// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Assets.Presentation.NodePresenters.Commands;
using Xenko.Assets.Presentation.ViewModel.Commands;
using Xenko.Assets.Scripts;

namespace Xenko.Assets.Presentation.ViewModel
{
    public class SymbolReferenceViewModel : AddReferenceViewModel
    {
        public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            Symbol symbol = null;
            bool singleChild = true;
            foreach (var child in children.Select(x => (NodeViewModel)x))
            {
                if (!singleChild)
                {
                    message = "Multiple symbols selected";
                    return false;
                }
                symbol = child.NodeValue as Symbol;
                if (symbol == null)
                {
                    message = "The selection is not a symbol";
                    return false;
                }

                singleChild = false;
            }
            if (symbol == null)
            {
                message = "The selection is not a symbol";
                return false;
            }
            message = $"Reference {symbol.Name}";
            return true;
        }

        public override void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var symbol = (Symbol)children.Select(x => (NodeViewModel)x).First().NodeValue;
            var command = TargetNode.GetCommand(SetSymbolReferenceCommand.CommandName);
            command.Execute(symbol);
        }
    }
}
