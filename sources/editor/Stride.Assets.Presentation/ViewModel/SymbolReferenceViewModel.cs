// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Assets.Presentation.NodePresenters.Commands;
using Stride.Assets.Presentation.ViewModel.Commands;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.ViewModel
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
