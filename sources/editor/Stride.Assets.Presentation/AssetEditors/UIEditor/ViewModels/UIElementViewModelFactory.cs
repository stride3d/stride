// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    internal sealed class UIElementViewModelFactory
    {
        private static readonly Lazy<UIElementViewModelFactory> LazyInstance = new Lazy<UIElementViewModelFactory>(() => new UIElementViewModelFactory());

        private readonly Dictionary<Type, Type> elementViewModelTypes = new Dictionary<Type, Type>();

        private UIElementViewModelFactory()
        {
            elementViewModelTypes[typeof(ContentControl)] = typeof(ContentControlViewModel);
            elementViewModelTypes[typeof(Panel)] = typeof(PanelViewModel);
        }

        public static UIElementViewModelFactory Instance => LazyInstance.Value;

        public UIElementViewModel ProvideViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (elementDesign == null) throw new ArgumentNullException(nameof(elementDesign));

            var elementViewModelType = typeof(UIElementViewModel);
            // try to get the viewmodel type
            var elementType = elementDesign.UIElement.GetType();
            while (elementType != null)
            {
                if (elementViewModelTypes.ContainsKey(elementType))
                {
                    elementViewModelType = elementViewModelTypes[elementType];
                    break;
                }

                elementType = elementType.BaseType;
            }

            return (UIElementViewModel)Activator.CreateInstance(elementViewModelType, editor, asset, elementDesign);
        }
    }
}
