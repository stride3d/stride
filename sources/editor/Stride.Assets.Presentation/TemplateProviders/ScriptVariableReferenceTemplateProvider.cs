// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Assets.Presentation.NodePresenters.Keys;
using Xenko.Assets.Presentation.NodePresenters.Updaters;
using Xenko.Assets.Scripts;

namespace Xenko.Assets.Presentation.TemplateProviders
{
    public class ScriptVariableReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => nameof(ScriptVariableReferenceTemplateProvider);

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Type == typeof(string) && (node.Root?.AssociatedData.ContainsKey(VisualScriptData.OwnerBlock) ?? false) && node.MemberInfo?.GetCustomAttribute<ScriptVariableReferenceAttribute>() != null;
        }
    }
}
