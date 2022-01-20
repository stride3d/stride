// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class DictionaryNumberKeyTemplateProvider : DictionaryTemplateProvider
    {
        public override string Name => "DictionaryNumberKey";

        /// <summary>
        /// If set to true, this provider will accept nodes representing entries of a number-keyed dictionary.
        /// Otherwise, it will accept nodes representing the number-keyed dictionary itself.
        /// </summary>
        public bool ApplyForItems { get; set; }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(sbyte),  typeof(byte),  typeof(short),
            typeof(ushort),  typeof(int),  typeof(uint),
            typeof(long), typeof(ulong),   typeof(nint),
            typeof(nuint), typeof(float),   typeof(double),
            typeof(decimal),
        };

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
                return NumericTypes.Contains(type);
            }

            return false;
        }
    }
}
