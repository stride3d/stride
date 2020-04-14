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
using System.IO;

namespace Stride.DebugTools
{
    /// <summary>
    /// Interaction logic for ScriptEditorControl.xaml
    /// </summary>
    public partial class ScriptEditorControl : UserControl
    {
        public ScriptEditorControl()
        {
            InitializeComponent();

            ReplTextBox.KeyDown += new KeyEventHandler(ReplTextBox_KeyDown);
            ReplTextBox.PreviewKeyDown += new KeyEventHandler(ReplTextBox_PreviewKeyDown);
        }

        private EngineContext engineContext;

        public void Initialize(EngineContext engineContext)
        {
            this.engineContext = engineContext;
        }

        List<string> replHistory = new List<string>();
        private int historyIndex = -1;

        void HistoryAdd(string command)
        {
            if (historyIndex != -1)
                replHistory[replHistory.Count - 1] = command;
            else
                replHistory.Add(command);
            if (replHistory.Last() == string.Empty)
                replHistory.RemoveAt(replHistory.Count - 1);
            historyIndex = -1;
        }

        void ReplTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle history (up/down)
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (historyIndex == -1)
                {
                    historyIndex = replHistory.Count;
                    replHistory.Add(ReplTextBox.Text);
                }
                else
                {
                    replHistory[historyIndex] = ReplTextBox.Text;
                }


                if (e.Key == Key.Up && historyIndex > 0)
                {
                    historyIndex--;
                    ReplTextBox.Text = replHistory[historyIndex];
                    ReplTextBox.SelectionStart = ReplTextBox.Text.Length;
                }
                if (e.Key == Key.Down && historyIndex < replHistory.Count - 1)
                {
                    historyIndex++;
                    ReplTextBox.Text = replHistory[historyIndex];
                    ReplTextBox.SelectionStart = ReplTextBox.Text.Length;
                }
            }
        }
        
        void ReplTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HistoryAdd(ReplTextBox.Text);

                try
                {
                    // Can't be done without JIT, so we should probably drop this feature
                    throw new NotSupportedException();
                }
                catch (Exception ex)
                {
                    ReplResults.AppendText(string.Format("Exception {0}: {1}", ex.GetType().FullName, ex.Message));
                }
                finally
                {
                    ReplTextBox.Text = string.Empty;
                    ReplResults.ScrollToEnd();
                }
            }
        }
    }
}
