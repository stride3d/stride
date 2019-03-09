// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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
using Xenko.Core.Presentation.Extensions;

namespace Xenko.Assets.Presentation.View
{
    /// <summary>
    /// Interaction logic for AddEntityComponentUserControl.xaml
    /// </summary>
    public partial class AddEntityComponentUserControl : UserControl
    {
        public const double ComponentListWidth = 500.0;

        public AddEntityComponentUserControl()
        {
            InitializeComponent();
            //FilteringComboBox.Loaded += ControlLoaded;
        }

        //private void ControlLoaded(object sender, RoutedEventArgs e)
        //{
        //    FilteringComboBox.SelectedIndex = -1;
        //    FilteringComboBox.Text = "";
        //    var groupList = this.FindVisualChildrenOfType<ListBox>().FirstOrDefault(x => x.Name == "GroupList");
        //    if (groupList != null)
        //        groupList.SelectedIndex = -1;
        //}
    }
}
