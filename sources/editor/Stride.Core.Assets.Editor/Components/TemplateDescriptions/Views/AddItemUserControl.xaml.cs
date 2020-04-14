// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views
{
    /// <summary>
    /// Interaction logic for AddAssetUserControl.xaml
    /// </summary>
    public partial class AddItemUserControl
    {
        public const double TemplateListWidth = 500.0;

        public static readonly DependencyProperty TemplateCollectionProperty = DependencyProperty.Register(nameof(TemplateCollection), typeof(AddItemTemplateCollectionViewModel), typeof(AddItemUserControl));

        public static readonly DependencyProperty AddItemCommandProperty = DependencyProperty.Register(nameof(AddItemCommand), typeof(ICommand), typeof(AddItemUserControl));

        public static readonly DependencyProperty SelectFilesToCreateItemCommandProperty = DependencyProperty.Register(nameof(SelectFilesToCreateItemCommand), typeof(ICommand), typeof(AddItemUserControl));

        public AddItemUserControl()
        {
            InitializeComponent();
            Loaded += ControlLoaded;
        }

        public AddItemTemplateCollectionViewModel TemplateCollection { get { return (AddItemTemplateCollectionViewModel)GetValue(TemplateCollectionProperty); } set { SetValue(TemplateCollectionProperty, value); } }

        public ICommand AddItemCommand { get { return (ICommand)GetValue(AddItemCommandProperty); } set { SetValue(AddItemCommandProperty, value); } }

        public ICommand SelectFilesToCreateItemCommand { get { return (ICommand)GetValue(SelectFilesToCreateItemCommandProperty); } set { SetValue(SelectFilesToCreateItemCommandProperty, value); } }

        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            FilteringComboBox.SelectedIndex = -1;
            FilteringComboBox.Text = "";
            var groupList = this.FindVisualChildrenOfType<ListBox>().FirstOrDefault(x => x.Name == "GroupList");
            if (groupList != null)
                groupList.SelectedIndex = -1;
        }
    }
}
