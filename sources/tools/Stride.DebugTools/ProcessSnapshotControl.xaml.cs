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
using Stride.Core.Presentation.Commands;
using Stride.DebugTools.DataStructures;
using Stride.Core.Presentation.Controls;

namespace Stride.DebugTools
{
    /// <summary>
    /// Interaction logic for ProcessSnapshotControl.xaml
    /// </summary>
    public partial class ProcessSnapshotControl : UserControl
    {
        private ProcessInfo processInfo;

        public ProcessSnapshotControl(ProcessInfo processInfo)
        {
            InitializeComponent();

            if (processInfo == null || processInfo.Frames.Count == 0)
                return;

            this.DataContext = this;

            this.processInfo = processInfo;
            this.Loaded += OnLoaded;

            timebar.BeforeTicksRender += new CustomRenderRoutedEventHandler(timebar_Layer0CustomRender);

            CreateTreeView();
        }

        private Pen pen = new Pen(Brushes.Red, 1.0);

        private void timebar_Layer0CustomRender(object sender, CustomRenderRoutedEventArgs e)
        {
            double pixel = processInfo.TimeLength * processInfoRenderer.PixelsPerSecond;
            e.DrawingContext.DrawLine(pen, new Point(pixel, 0.0), new Point(pixel, timebar.ActualHeight));
        }

        private void CreateTreeView()
        {
            if (processInfo == null)
                return;

            foreach (FrameInfo frame in processInfo.Frames)
            {
                TreeViewItem tviFrame = new TreeViewItem { Header = string.Format("Frame {0} ({1} - {2})", frame.FrameNumber, frame.BeginTime, frame.EndTime) };

                foreach (ThreadInfo thread in frame.ThreadItems)
                {
                    TreeViewItem tviThread = new TreeViewItem { Header = string.Format("Thread {0}", thread.Id) };

                    foreach (MicroThreadInfo mt in thread.MicroThreadItems)
                    {
                        TreeViewItem tviMicroThread = new TreeViewItem { Header = string.Format("MicroThread {0} ({1} - {2})", mt.Id, mt.BeginTime, mt.EndTime) };
                        tviThread.Items.Add(tviMicroThread);
                    }

                    tviFrame.Items.Add(tviThread);
                }

                treeView.Items.Add(tviFrame);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (processInfo == null)
                return;

            FrameInfo lastFrame = processInfo.Frames.Last();

            processInfoRenderer.SizeChanged += (ss, ee) => timebar.SetUnitAt(lastFrame.EndTime, processInfoRenderer.ActualWidth);

            timebar.BeforeRender += (ss, ee) =>
            {
                processInfoRenderer.Width = processInfo.TimeLength * timebar.PixelsPerUnit;
                processInfoRenderer.PixelsPerSecond = timebar.PixelsPerUnit;
                processInfoRenderer.RenderAllFrames(processInfo);
            };

            processInfoRenderer.RenderAllFrames(processInfo);

            scrollViewer.ScrollToRightEnd();
        }

        private void timebar_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(timebar);
            Window.GetWindow(this).Title = timebar.GetUnitAt(pos.X).ToString();
        }
    }
}
