// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Presentation.TemplateProviders
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
