// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Extensions;

namespace Stride.Assets.Presentation.View
{
    /// <summary>
    /// Interaction logic for AddEntityComponentUserControl.xaml
    /// </summary>
    public partial class AddEntityComponentUserControl : UserControl
    {
        public const double ComponentListWidth = 250.0;


        public IEnumerable<AbstractNodeType> AvailableComponentTypes
        {
            get { return (IEnumerable<AbstractNodeType>)GetValue(AvailableComponentTypesProperty); }
            set { SetValue(AvailableComponentTypesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComponentTypes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AvailableComponentTypesProperty =
            DependencyProperty.Register("AvailableComponentTypes", typeof(IEnumerable<AbstractNodeType>), typeof(AddEntityComponentUserControl), new PropertyMetadata(null));

        public IEnumerable<AbstractNodeTypeGroup> AvailableComponentTypeGroups
        {
            get { return (IEnumerable<AbstractNodeTypeGroup>)GetValue(AvailableComponentTypeGroupsProperty); }
            set { SetValue(AvailableComponentTypeGroupsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AvailableComponentTypeGroups.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AvailableComponentTypeGroupsProperty =
            DependencyProperty.Register("AvailableComponentTypeGroups", typeof(IEnumerable<AbstractNodeTypeGroup>), typeof(AddEntityComponentUserControl), new PropertyMetadata(null));

        public AbstractNodeTypeGroup SelectedGroup
        {
            get { return (AbstractNodeTypeGroup)GetValue(SelectedGroupProperty); }
            set { SetValue(SelectedGroupProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedGroup.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedGroupProperty =
            DependencyProperty.Register("SelectedGroup", typeof(AbstractNodeTypeGroup), typeof(AddEntityComponentUserControl), new PropertyMetadata(null, OnSelectedGroupChanged));

        public string SearchToken
        {
            get { return (string)GetValue(SearchTokenProperty); }
            set { SetValue(SearchTokenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SearchToken.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchTokenProperty =
            DependencyProperty.Register("SearchToken", typeof(string), typeof(AddEntityComponentUserControl), new PropertyMetadata(null,OnSearchTokenChanged));        

        public IEnumerable<AbstractNodeType> ComponentTypes
        {
            get { return (IEnumerable<AbstractNodeType>)GetValue(ComponentTypesProperty); }
            set { SetValue(ComponentTypesProperty, value); }
        }

        public static readonly DependencyProperty ComponentTypesProperty =
            DependencyProperty.Register("ComponentTypes", typeof(IEnumerable<AbstractNodeType>), typeof(AddEntityComponentUserControl), new PropertyMetadata(null));

        public ICommand AddNewItemCommand
        {
            get { return (ICommand)GetValue(AddNewItemCommandProperty); }
            set { SetValue(AddNewItemCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddNewItemCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddNewItemCommandProperty =
            DependencyProperty.Register("AddNewItemCommand", typeof(ICommand), typeof(AddEntityComponentUserControl), new PropertyMetadata(null));

        public ICommand AddNewScriptComponentCommand
        {
            get { return (ICommand)GetValue(AddNewScriptComponentCommandProperty); }
            set { SetValue(AddNewScriptComponentCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AddNewScriptComponentCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AddNewScriptComponentCommandProperty =
            DependencyProperty.Register("AddNewScriptComponentCommand", typeof(ICommand), typeof(AddEntityComponentUserControl), new PropertyMetadata(null));

        private static void OnSelectedGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AddEntityComponentUserControl control)
            {
                d.SetValue(ComponentTypesProperty, e.NewValue is AbstractNodeTypeGroup group ? group.Types : null);
            }
        }

        private static void OnSearchTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AddEntityComponentUserControl control)
            {
                if(string.IsNullOrEmpty(e.NewValue as string))
                {
                    control.ComponentTypes = null;
                }
                else
                {
                    control.ComponentTypes = control.AvailableComponentTypes;
                }
            }
        }

        public AddEntityComponentUserControl()
        {
            InitializeComponent();
            FilteringComboBox.DataContext = this;

            Popup.Closed += Popup_Closed;
        }

        private void Popup_Closed(object sender, EventArgs e)
        {
            FilteringComboBox.SelectedIndex = -1;
            SearchToken = null;
            SelectedGroup = null;
            var groupList = FilteringComboBox.FindVisualChildrenOfType<ListBox>().FirstOrDefault(x => x.Name == "GroupList");
            if (groupList != null)
                groupList.SelectedIndex = -1;
        }
    }
}
