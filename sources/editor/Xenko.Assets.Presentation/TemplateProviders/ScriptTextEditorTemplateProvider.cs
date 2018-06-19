// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using Xenko.Engine;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Assets.Scripts;

namespace Xenko.Assets.Presentation.TemplateProviders
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
