// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class DictionaryStringKeyTemplateProvider : DictionaryTemplateProvider
    {
        public override string Name => "DictionaryStringKey";

        /// <summary>
        /// If set to true, this provider will accept nodes representing entries of a string-keyed dictionary.
        /// Otherwise, it will accept nodes representing the string-keyed dictionary itself.
        /// </summary>
        public bool ApplyForItems { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
            if (ApplyForItems)
            {
                node = node.Parent;
                if (node == null)
                    return false;
            }

            if (!base.MatchNode(node))
                return false;

            if (node.AssociatedData.TryGetValue(DictionaryNodeUpdater.DictionaryNodeKeyType.Name, out var value))
            {
                var type = (Type)value;
                return type == typeof(string);
            }

            return false;
        }
    }
}
