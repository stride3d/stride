// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Presentation.Quantum.ViewModels
{
    public class NodeViewModelFactory : INodeViewModelFactory
    {
        public NodeViewModel CreateGraph(GraphViewModel owner, Type rootType, IEnumerable<INodePresenter> rootNodes)
        {
            var rootViewModelNode = CreateNodeViewModel(owner, null, rootType, rootNodes.ToList(), true);
            return rootViewModelNode;
        }

        public void GenerateChildren(GraphViewModel owner, NodeViewModel parent, List<INodePresenter> nodePresenters)
        {
            foreach (var child in CombineChildren(nodePresenters))
            {
                if (ShouldConstructViewModel(child))
                {
                    Type type = null;
                    var typeMatch = true;
                    foreach (var childPresenter in child)
                    {
                        if (type == null)
                        {
                            type = childPresenter.Type;
                        }
                        else if (type != childPresenter.Type && type.IsAssignableFrom(childPresenter.Type))
                        {
                            type = childPresenter.Type;
                        }
                        else if (type != childPresenter.Type)
                        {
                            typeMatch = false;
                            break;
                        }
                    }
                    if (typeMatch)
                    {
                        CreateNodeViewModel(owner, parent, child.First().Type, child);
                    }
                }
            }
        }

        [NotNull]
        protected virtual NodeViewModel CreateNodeViewModel([NotNull] GraphViewModel owner, NodeViewModel parent, Type nodeType, [NotNull] List<INodePresenter> nodePresenters, bool isRootNode = false)
        {
            // TODO: properly compute the name
            var viewModel = new NodeViewModel(owner, parent, nodePresenters.First().Name, nodeType, nodePresenters);
            if (isRootNode)
            {
                owner.RootNode = viewModel;
            }
            viewModel.Refresh();
            return viewModel;
        }

        [NotNull]
        protected virtual IEnumerable<List<INodePresenter>> CombineChildren([NotNull] List<INodePresenter> nodePresenters)
        {
            var dictionary = new Dictionary<string, List<INodePresenter>>();
            foreach (var nodePresenter in nodePresenters)
            {
                foreach (var child in nodePresenter.Children)
                {
                    List<INodePresenter> presenters;
                    // TODO: properly implement CombineKey
                    if (!dictionary.TryGetValue(child.CombineKey, out presenters))
                    {
                        presenters = new List<INodePresenter>();
                        dictionary.Add(child.CombineKey, presenters);
                    }
                    presenters.Add(child);
                }
            }
            return dictionary.Values.Where(x => x.Count == nodePresenters.Count);
        }

        private static bool ShouldConstructViewModel([NotNull] List<INodePresenter> nodePresenters)
        {
            foreach (var nodePresenter in nodePresenters)
            {
                var member = nodePresenter as MemberNodePresenter;
                var displayAttribute = member?.MemberAttributes.OfType<DisplayAttribute>().FirstOrDefault();
                if (displayAttribute != null && !displayAttribute.Browsable)
                    return false;
            }
            return true;
        }
    }
}
