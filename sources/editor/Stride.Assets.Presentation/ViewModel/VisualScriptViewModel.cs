// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Collections;
using Stride.Core.Quantum;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// View model for a <see cref="VisualScriptAsset"/>.
    /// </summary>
    [AssetViewModel(typeof(VisualScriptAsset))]
    public class VisualScriptViewModel : AssetCompositeViewModel<VisualScriptAsset>
    {
        private readonly IObjectNode propertiesContent;
        private readonly IObjectNode methodsContent;

        public VisualScriptViewModel(AssetViewModelConstructionParameters parameters) : base(parameters)
        {
            var rootNode = Session.AssetNodeContainer.GetOrCreateNode(Asset);
            propertiesContent = rootNode[nameof(VisualScriptAsset.Properties)].Target;
            methodsContent = rootNode[nameof(VisualScriptAsset.Methods)].Target;

            methodsContent.ItemChanged += MethodsContentChanged;
            foreach (var method in Asset.Methods)
                Methods.Add(new VisualScriptMethodViewModel(this, method));
        }

        public ObservableList<VisualScriptMethodViewModel> Methods { get; } = new ObservableList<VisualScriptMethodViewModel>();

        public void AddProperty(Property property)
        {
            propertiesContent.Add(property);
        }

        public void RemoveProperty(Property property)
        {
            // Find index
            var index = Asset.Properties.IndexOf(property);
            if (index < 0)
                return;

            // TODO: Cleanup references to this variable

            // Remove
            var itemIndex = new NodeIndex(index);
            propertiesContent.Remove(property, itemIndex);
        }

        public void AddMethod(Method method)
        {
            methodsContent.Add(method);
        }

        public void RemoveMethod(Method method)
        {
            // Find index
            var index = Asset.Methods.IndexOf(method);
            if (index < 0)
                return;

            // TODO: Cleanup references to this function

            // Remove
            var itemIndex = new NodeIndex(index);
            methodsContent.Remove(method, itemIndex);
        }

        /// <inheritdoc/>
        protected override IObjectNode GetPropertiesRootNode()
        {
            // We don't use CanProvideProperties because we still want the button to open in editor. But we don't want to display any property directly
            return null;
        }

        private void MethodsContentChanged(object sender, ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.None:
                case ContentChangeType.CollectionAdd:
                    {
                        var function = (Method)e.NewValue;
                        Methods.Insert(e.Index.Int, new VisualScriptMethodViewModel(this, function));
                        break;
                    }
                case ContentChangeType.CollectionRemove:
                    {
                        var viewModel = Methods[e.Index.Int];
                        viewModel?.Destroy();
                        Methods.RemoveAt(e.Index.Int);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
