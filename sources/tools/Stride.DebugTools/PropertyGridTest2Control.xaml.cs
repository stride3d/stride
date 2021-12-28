// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Stride.Framework.ViewModel;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;
using Stride.Framework.Diagnostics;
using Stride.EntityModel;
using System.ComponentModel;
using Stride.DebugTools.ViewModels;
using System.Collections;
using Stride.Core.Presentation.Extensions;

namespace Stride.DebugTools
{
    /// <summary>
    /// Interaction logic for PropertyGridTest2Control.xaml
    /// </summary>
    public partial class PropertyGridTest2Control : UserControl
    {
        public PropertyGridTest2Control()
        {
            InitializeComponent();

            MyDateTime now = MyDateTime.FromDateTime(DateTime.Now);

            Components = new Component[]
            {
                CreateComponent("DateTime", now),
                CreateComponent("Matrix", TestMatrix.CreateIdentity()),
            };

            this.DataContext = this;
        }

        public object Components { get; private set; }

        private IViewModelNode CreateProperty(object obj)
        {
            var context = new ViewModelContext();
            var contextUI = new ViewModelContext();

            context.ChildrenPropertyEnumerators.Add(new ChildrenPropertyInfoEnumerator());
            // add some more here...

            var testModel = new ViewModelNode("Root", obj);

            var view = ObservableViewModelNode.CreateObservableViewModel(contextUI, testModel);

            ObservableViewModelNode.Refresh(contextUI, context, new ViewModelState());

            return view;
        }

        private Component CreateComponent(string name, object content)
        {
            return new Component
            {
                Name = name,
                Properties = new IViewModelNode[] { CreateProperty(content) },
            };
        }
    }


    public class CustomTemplateSelector : DataTemplateSelector
    {
        private static DataTemplate errorTemplate;
        private static DataTemplate componentTemplate;
        private static DataTemplate viewModelTemplate;
        private static DataTemplate textTemplate;

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate template = null;

            var element = container as FrameworkElement;
            if (element == null)
                throw new Exception("Container must be of type FrameworkElement");

            if (item is ViewModelReference && ((ViewModelReference)item).ViewModel.PropertyName == "EntityComponent")
            {
                if (componentTemplate == null)
                    componentTemplate = (DataTemplate)element.FindResource("EntityComponentView");
                template = componentTemplate;
            }
            else if (item is ViewModelReference)
            {
                return (DataTemplate)element.FindResource("ViewModelReference");
            }
            else if (item is IViewModelNode)
            {
                var viewModel = (IViewModelNode)item;

                if (viewModel.Children.Count > 0)
                {
                    if (viewModelTemplate == null)
                        viewModelTemplate = (DataTemplate)element.FindResource("IViewModelNode");
                    template = viewModelTemplate;
                }
                else if (viewModel.Type == typeof(ViewModelReference))
                {
                    if (viewModel.PropertyName == "ObjectRef")
                        template = (DataTemplate)element.FindResource("ViewModelReferenceNode");
                    else
                        template = (DataTemplate)element.FindResource("ViewModelReferenceGuid");
                }
                else if (viewModel.Type == typeof(IList<ViewModelReference>))
                {
                    template = (DataTemplate)element.FindResource("ListViewModelReference");
                }
                else
                {
                    if (textTemplate == null)
                        textTemplate = (DataTemplate)element.FindResource("TextBox");
                    template = textTemplate;
                }
            }

            if (template == null)
            {
                if (errorTemplate == null)
                    errorTemplate = (DataTemplate)element.FindResource("Error");
                template = errorTemplate;
            }

            return template;
        }
    }

    public class Component
    {
        public string Name { get; set; }
        public IEnumerable<Component> Components { get; set; }
        public IEnumerable<IViewModelNode> Properties { get; set; }
    }

    public struct TestMatrix
    {
        public float M11 { get; set; }
        public float M12 { get; set; }
        public float M13 { get; set; }
        public float M14 { get; set; }

        public float M21 { get; set; }
        public float M22 { get; set; }
        public float M23 { get; set; }
        public float M24 { get; set; }

        public float M31 { get; set; }
        public float M32 { get; set; }
        public float M33 { get; set; }
        public float M34 { get; set; }

        public float M41 { get; set; }
        public float M42 { get; set; }
        public float M43 { get; set; }
        public float M44 { get; set; }

        public static TestMatrix CreateIdentity()
        {
            return new TestMatrix
            {
                M11 = 1.0f,
                M12 = 0.0f,
                M13 = 0.0f,
                M14 = 0.0f,
                M21 = 0.0f,
                M22 = 1.0f,
                M23 = 0.0f,
                M24 = 0.0f,
                M31 = 0.0f,
                M32 = 0.0f,
                M33 = 1.0f,
                M34 = 0.0f,
                M41 = 0.0f,
                M42 = 0.0f,
                M43 = 0.0f,
                M44 = 1.0f,
            };
        }
    }
}
