// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Globalization;
using Stride.DebugTools.DataStructures;
using Stride.Core.Presentation.Core;

namespace Stride.DebugTools
{
    public class RenderRoutedEventArgs : RoutedEventArgs
    {
        public ProcessInfo ProcessData { get; set; }

        public RenderRoutedEventArgs()
        {
            RoutedEvent = ProcessInfoRenderer.RenderEvent;
        }
    }

    public class FrameRenderRoutedEventArgs : RenderRoutedEventArgs
    {
        public FrameInfo FrameData { get; set; }

        public FrameRenderRoutedEventArgs()
        {
            RoutedEvent = ProcessInfoRenderer.LastFrameRenderEvent;
        }
    }

    /// <summary>
    /// Renders a full micro threading process.
    /// </summary>
    public class ProcessInfoRenderer : Canvas
    {
        public static readonly DependencyProperty ThreadLineHeightProperty = DependencyProperty.Register("ThreadLineHeight", typeof(double), typeof(ProcessInfoRenderer),
            new FrameworkPropertyMetadata(32.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ThreadLineHeightMarginProperty = DependencyProperty.Register("ThreadLineHeightMargin", typeof(double), typeof(ProcessInfoRenderer),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty PixelsPerSecondProperty = DependencyProperty.Register("PixelsPerSecond", typeof(double), typeof(ProcessInfoRenderer),
            new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly DependencyPropertyKey LastFrameTimePropertyKey = DependencyProperty.RegisterReadOnly("LastFrameTime", typeof(double), typeof(ProcessInfoRenderer), new PropertyMetadata());
        public static readonly DependencyProperty LastFrameTimeProperty = LastFrameTimePropertyKey.DependencyProperty;

        private readonly Brush defaultBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x64, 0x95, 0xED));
        private readonly Pen defaultPen = new Pen(Brushes.Black, 0.4);
        private readonly Pen delimiterPen = new Pen(Brushes.LightGray, 0.5);

        public delegate void RenderRoutedEventHandler(object sender, RenderRoutedEventArgs e);
        public static readonly RoutedEvent RenderEvent = EventManager.RegisterRoutedEvent("Render", RoutingStrategy.Bubble, typeof(RenderRoutedEventHandler), typeof(ProcessInfoRenderer));

        public delegate void FrameRenderRoutedEventHandler(object sender, FrameRenderRoutedEventArgs e);
        public static readonly RoutedEvent LastFrameRenderEvent = EventManager.RegisterRoutedEvent("LastFrameRender", RoutingStrategy.Bubble, typeof(FrameRenderRoutedEventHandler), typeof(ProcessInfoRenderer));

        public event RenderRoutedEventHandler Render
        {
            add { AddHandler(RenderEvent, value); }
            remove { RemoveHandler(RenderEvent, value); }
        }

        public event FrameRenderRoutedEventHandler LastFrameRender
        {
            add { AddHandler(LastFrameRenderEvent, value); }
            remove { RemoveHandler(LastFrameRenderEvent, value); }
        }

        private void RaiseRenderEvent(ProcessInfo processData)
        {
            RaiseEvent(new RenderRoutedEventArgs { ProcessData = processData });
        }

        private void RaiseLastFrameRenderEvent(ProcessInfo processData, FrameInfo frameData)
        {
            RaiseEvent(new FrameRenderRoutedEventArgs { ProcessData = processData, FrameData = frameData });
        }

        public double ThreadLineHeight
        {
            get { return (double)GetValue(ThreadLineHeightProperty); }
            set { SetValue(ThreadLineHeightProperty, value); }
        }

        public double ThreadLineHeightMargin
        {
            get { return (double)GetValue(ThreadLineHeightMarginProperty); }
            set { SetValue(ThreadLineHeightMarginProperty, value); }
        }

        public double PixelsPerSecond
        {
            get { return (double)GetValue(PixelsPerSecondProperty); }
            set { SetValue(PixelsPerSecondProperty, value); }
        }

        public ProcessInfoRenderer()
        {
            defaultBrush.Freeze();
            defaultPen.Freeze();
        }

        /// <summary>
        /// This method moves the previously rendered frame and only renders the newly added one.
        /// It automatically removes the first frame when the maximum frame count is reached.
        /// </summary>
        /// <param name="processInfo">Instance that stores the whole micro threading process data.</param>
        /// <param name="alignRight">Indicates whether render is right aligned or left aligned.
        /// <remarks>Right align produces more realistic time-related render.</remarks>
        /// </param>
        public void RenderLastFrame(ProcessInfo processInfo, bool alignRight = true)
        {
            if (processInfo == null)
                throw new ArgumentNullException("processInfo");

            if (processInfo.Frames.Count == 0)
                return;

            if (Children.Count >= MicroThreadMonitoringManager.MaximumCapturedFrames)
                Children.RemoveAt(0);

            FrameInfo lastFrame = processInfo.Frames.Last();
            SetValue(LastFrameTimePropertyKey, lastFrame.EndTime);

            double offset = processInfo.Frames[0].BeginTime;

            if (alignRight)
                offset = lastFrame.EndTime - (ActualWidth / PixelsPerSecond);

            for (int i = 0; i < Children.Count; i++)
                Children[i].SetValue(Canvas.LeftProperty, (processInfo.Frames[i].BeginTime - offset) * PixelsPerSecond);

            UIElement frameControl = CreateFrameElement(lastFrame);

            if (frameControl != null)
            {
                frameControl.SetValue(Canvas.LeftProperty, (lastFrame.BeginTime - offset) * PixelsPerSecond);
                Children.Add(frameControl);
            }

            RaiseLastFrameRenderEvent(processInfo, lastFrame);
        }

        /// <summary>
        /// Clears any previous render and perform a new one from scratch.
        /// </summary>
        /// <param name="processInfo">Instance that stores the whole micro threading process data.</param>
        public void RenderAllFrames(ProcessInfo processInfo)
        {
            if (processInfo == null)
                throw new ArgumentNullException("processInfo");

            if (processInfo.Frames.Count == 0)
                return;

            Children.Clear();

            double offset = processInfo.Frames[0].BeginTime;

            foreach (FrameInfo frame in processInfo.Frames)
            {
                UIElement frameControl = CreateFrameElement(frame);

                if (frameControl != null)
                {
                    frameControl.SetValue(Canvas.LeftProperty, (frame.BeginTime - offset) * PixelsPerSecond);
                    Children.Add(frameControl);
                }
            }

            RaiseRenderEvent(processInfo);
        }

        private int maxThreadCount = 0;

        /// <summary>
        /// Creates the render elements of all threads over one frame.
        /// </summary>
        /// <param name="frame">Instance that stores all thread data.</param>
        /// <returns>Returns a <c>Panel</c> (<c>Canvas</c>) containing rendered thread elements for the given frame.</returns>
        private FrameworkElement CreateFrameElement(FrameInfo frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame");

            if (frame.ThreadItems.Count == 0)
                return null;

            if (frame.ThreadItems.Count > maxThreadCount)
            {
                maxThreadCount = frame.ThreadItems.Count;
                InvalidateVisual();
            }

            Canvas canvas = new Canvas();

            int threadIndex = 0;
            foreach (ThreadInfo thread in frame.ThreadItems)
            {
                UIElement frameThread = CreateFrameThreadElement(frame.BeginTime, thread);

                if (frameThread != null)
                {
                    frameThread.SetValue(Canvas.TopProperty, threadIndex * ThreadLineHeight);
                    canvas.Children.Add(frameThread);
                }

                threadIndex++;
            }

            VisualElement frameDelimiter = new VisualElement(CreateLine(maxThreadCount * ThreadLineHeight));
            frameDelimiter.SetValue(Canvas.LeftProperty, -(delimiterPen.Thickness / 2.0));
            canvas.Children.Add(frameDelimiter);

            double w = (frame.EndTime - frame.BeginTime) * PixelsPerSecond;
            canvas.Children.Add(new VisualElement(CreateText(frame.FrameNumber.ToString(), w, ThreadLineHeight)));

            return canvas;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            for (int i = 0; i < maxThreadCount + 1; i++)
            {
                double y = i * ThreadLineHeight;
                dc.DrawLine(delimiterPen, new Point(0.0, y), new Point(ActualWidth, y));
            }
        }

        /// <summary>
        /// Creates the render element for a thread over one frame.
        /// </summary>
        /// <param name="frameBeginTime">Time offset (in second) to align all micro threads render on the frame time.</param>
        /// <param name="thread">Instance that stores all micro thread events.</param>
        /// <returns>Returns a <c>UIElement</c> (<c>VisualContainerElement</c>) containing rendered micro thread elements for the given thread.</returns>
        private UIElement CreateFrameThreadElement(double frameBeginTime, ThreadInfo thread)
        {
            if (thread == null)
                throw new ArgumentNullException("thread");

            if (thread.MicroThreadItems.Count == 0)
                return null;

            double y = ThreadLineHeightMargin;
            double h = ThreadLineHeight - ThreadLineHeightMargin * 2.0;

            VisualContainerElement container = new VisualContainerElement();

            foreach (MicroThreadInfo microThread in thread.MicroThreadItems)
            {
                double x = (microThread.BeginTime - frameBeginTime) * PixelsPerSecond;
                double w = (microThread.EndTime - microThread.BeginTime) * PixelsPerSecond;

                if (w <= 0.0)
                    continue;

                container.AddVisual(CreateRectangle(x, y, w, h));
            }

            return container;
        }

        private Visual CreateRectangle(double x, double y, double w, double h)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(defaultBrush, defaultPen, new Rect(x, y, w, h));
            }

            return visual;
        }

        private Visual CreateLine(double h)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawLine(delimiterPen, new Point(0.0, 0.0), new Point(0.0, h));
            }

            return visual;
        }

        private Visual CreateText(string text, double w, double h)
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                FormattedText ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 18.0, Brushes.Gray);
                context.DrawText(ft, new Point((w - ft.Width) / 2.0, (h - ft.Height) / 2.0));
            }

            return visual;
        }

        /*
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            bool applyMatrix = true;
            Matrix m = LayoutTransform.Value;

            Point pos = e.GetPosition(this);

            if (e.Delta > 0)
            {
                m.ScaleAt(1.5, 1.5, pos.X, pos.Y);
            }
            else
            {
                m.ScaleAt(1.0 / 1.5, 1.0 / 1.5, pos.X, pos.Y);
                if (m.M11 < 1.0)
                    applyMatrix = false;
            }

            if (applyMatrix)
                LayoutTransform = new MatrixTransform(m);

            base.OnMouseWheel(e);
        }
        */
    }
}
