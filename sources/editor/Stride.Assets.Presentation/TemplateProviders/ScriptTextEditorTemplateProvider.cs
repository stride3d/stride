// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using Stride.Engine;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class ScriptTextEditorTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "ScriptTextEditor";

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Type == typeof(string) && node.MemberInfo?.GetCustomAttribute<ScriptCodeAttribute>() != null;
        }
    }
}
