// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

using Stride.Core.Presentation.Graph.Behaviors;
using Stride.Core.Presentation.Graph.ViewModel;
using System.Windows.Media;
using GraphX.Controls;
using Stride.Core.Collections;

namespace Stride.Core.Presentation.Graph.Controls
{
    /// <summary>
    /// 
    /// </summary>
    [TemplatePart(Name = "PART_inputItemsControl", Type = typeof(ItemsControl))]
    [TemplatePart(Name = "PART_outputItemsControl", Type = typeof(ItemsControl))]
    public class NodeVertexControl : VertexControl, INotifyPropertyChanged, ConnectorDropBehavior.IDropHandler, ActiveConnectorBehavior.IActiveConnectorHandler
    {
        #region Dependency Properties
        public static DependencyProperty InputSlotsProperty = DependencyProperty.Register("InputSlots", typeof(IEnumerable), typeof(NodeVertexControl), new PropertyMetadata(OnInputSlotsChanged));
        public static DependencyProperty OutputSlotsProperty = DependencyProperty.Register("OutputSlots", typeof(IEnumerable), typeof(NodeVertexControl), new PropertyMetadata(OnOutputSlotsChanged));

        public static readonly DependencyProperty TitleBackgroundProperty = DependencyProperty.Register("TitleBackground", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.DarkGray));
        public static readonly DependencyProperty TitleBorderBrushProperty = DependencyProperty.Register("TitleBorderBrush", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.DarkGray));
        public static readonly DependencyProperty TitleBorderThicknessProperty = DependencyProperty.Register("TitleBorderThickness", typeof(double), typeof(NodeVertexControl), new PropertyMetadata(2.0));
        public static readonly DependencyProperty TitleBorderCornerRadiusProperty = DependencyProperty.Register("TitleBorderCornerRadius", typeof(CornerRadius), typeof(NodeVertexControl), new PropertyMetadata(new CornerRadius(5, 5, 0, 0)));
        public static readonly DependencyProperty TitlePaddingProperty = DependencyProperty.Register("TitlePadding", typeof(Thickness), typeof(NodeVertexControl), new PropertyMetadata(new Thickness()));
    
        public static readonly DependencyProperty ContentBackgroundProperty = DependencyProperty.Register("ContentBackground", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.Gray));
        public static readonly DependencyProperty ContentBorderBrushProperty = DependencyProperty.Register("ContentBorderBrush", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.Gray));
        public static readonly DependencyProperty ContentBorderThicknessProperty = DependencyProperty.Register("ContentBorderThickness", typeof(double), typeof(NodeVertexControl), new PropertyMetadata(2.0));
        public static readonly DependencyProperty ContentBorderCornerRadiusProperty = DependencyProperty.Register("ContentBorderCornerRadius", typeof(CornerRadius), typeof(NodeVertexControl), new PropertyMetadata(new CornerRadius(0, 0, 5, 5)));
        public static readonly DependencyProperty ContentPaddingProperty = DependencyProperty.Register("ContentPadding", typeof(Thickness), typeof(NodeVertexControl), new PropertyMetadata(new Thickness()));

        public static readonly DependencyProperty SelectedTitleBackgroundProperty = DependencyProperty.Register("SelectedTitleBackground", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.LightGreen));
        public static readonly DependencyProperty SelectedContentBackgroundProperty = DependencyProperty.Register("SelectedContentBackground", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.Gray));

        public static readonly DependencyProperty ConnectorStrokeProperty = DependencyProperty.Register("ConnectorStroke", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.DarkGray));
        public static readonly DependencyProperty ConnectorFillProperty = DependencyProperty.Register("ConnectorFill", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.LightGray));
        public static readonly DependencyProperty MouseOverConnectorFillProperty = DependencyProperty.Register("MouseOverConnectorFill", typeof(Brush), typeof(NodeVertexControl), new PropertyMetadata(Brushes.Green));
        #endregion

        #region Members
        private TrackingDictionary<object, DependencyObject> connectors_ = new TrackingDictionary<object, DependencyObject>();
        #endregion

        #region Static Dependency Property Event Handler
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnInputSlotsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = (NodeVertexControl)obj;
            control.InputSlots = e.NewValue as IEnumerable;
        }

        /// <summary>
        /// 
        /// </summary>o
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnOutputSlotsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var control = (NodeVertexControl)obj;
            control.OutputSlots = e.NewValue as IEnumerable;
        }
        #endregion

        #region Members
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructor
        /// <summary>
        /// Create vertex visual control
        /// </summary>
        /// <param name="vertexData">Vertex data object</param>
        /// <param name="tracePositionChange">Listen for the vertex position changed events and fire corresponding event</param>
        /// <param name="bindToDataObject">Bind DataContext to the Vertex data. True by default. </param>
        public NodeVertexControl(object vertexData, bool tracePositionChange = true, bool bindToDataObject = true)
            : base(vertexData, tracePositionChange, bindToDataObject)
        {
        }
        #endregion

        #region Event Handlers
        public void DisableDrag()
        {
            DragBehaviour.SetIsDragEnabled(this, false);
        }

        public void EnableDrag()
        {
            DragBehaviour.SetIsDragEnabled(this, true);
        }
        #endregion

        #region ActiveConnectorBehavior Handlers

        void ActiveConnectorBehavior.IActiveConnectorHandler.OnAttached(FrameworkElement slot)
        {
            connectors_.Add(slot.DataContext, slot);
        }

        void ActiveConnectorBehavior.IActiveConnectorHandler.OnDetached(FrameworkElement slot)
        {
            connectors_.Remove(slot.DataContext);
        }

        #endregion

        #region ConnectorDropBehavior Drop Handlers
        /// <summary>
        /// On drag over event handling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ConnectorDropBehavior.IDropHandler.OnDragOver(object sender, DragEventArgs e)
        {
            //var model = e.Data.GetData(typeof(ModelViewModel));
            var model = e.Data.GetData(typeof(object));
            if (model != null)
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }

            //e.Effects = DragDropEffects.Link;
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        /// <summary>
        /// On drop event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ConnectorDropBehavior.IDropHandler.OnDrop(object sender, DragEventArgs e)
        {
            var item = e.Data.GetData(typeof(Tuple<NodeVertex, object>)) as Tuple<NodeVertex, object>;

            NodeVertex source = item.Item1;
            NodeVertex target = Vertex as NodeVertex;

            source.AddOutgoing(target, item.Item2, (sender as FrameworkElement).DataContext);
        }
        #endregion

        #region Properties
        public IEnumerable InputSlots { get { return (IEnumerable)GetValue(InputSlotsProperty); } set { SetValue(InputSlotsProperty, value); } }
        public IEnumerable OutputSlots { get { return (IEnumerable)GetValue(OutputSlotsProperty); } set { SetValue(OutputSlotsProperty, value); } }
        
        public Brush TitleBackground { get { return (Brush)GetValue(TitleBackgroundProperty); } set { SetValue(TitleBackgroundProperty, value); } }
        public Brush TitleBorderBrush { get { return (Brush)GetValue(TitleBorderBrushProperty); } set { SetValue(TitleBorderBrushProperty, value); } }
        public double TitleBorderThickness { get { return (double)GetValue(TitleBorderThicknessProperty); } set { SetValue(TitleBorderThicknessProperty, value); } }
        public CornerRadius TitleBorderCornerRadius { get { return (CornerRadius)GetValue(TitleBorderCornerRadiusProperty); } set { SetValue(TitleBorderCornerRadiusProperty, value); } }
        public Thickness TitlePadding { get { return (Thickness)GetValue(TitlePaddingProperty); } set { SetValue(TitlePaddingProperty, value); } }        
        
        public Brush ContentBackground { get { return (Brush)GetValue(ContentBackgroundProperty); } set { SetValue(ContentBackgroundProperty, value); } }
        public Brush ContentBorderBrush { get { return (Brush)GetValue(ContentBorderBrushProperty); } set { SetValue(ContentBorderBrushProperty, value); } }
        public double ContentBorderThickness { get { return (double)GetValue(ContentBorderThicknessProperty); } set { SetValue(ContentBorderThicknessProperty, value); } }
        public CornerRadius ContentBorderCornerRadius { get { return (CornerRadius)GetValue(ContentBorderCornerRadiusProperty); } set { SetValue(ContentBorderCornerRadiusProperty, value); } }
        public Thickness ContentPadding { get { return (Thickness)GetValue(ContentPaddingProperty); } set { SetValue(ContentPaddingProperty, value); } }
        
        public Brush SelectedTitleBackground { get { return (Brush)GetValue(SelectedTitleBackgroundProperty); } set { SetValue(SelectedTitleBackgroundProperty, value); } }
        public Brush SelectedContentBackground { get { return (Brush)GetValue(SelectedContentBackgroundProperty); } set { SetValue(SelectedContentBackgroundProperty, value); } }

        public Brush ConnectorStroke { get { return (Brush)GetValue(ConnectorStrokeProperty); } set { SetValue(ConnectorStrokeProperty, value); } }
        public Brush ConnectorFill { get { return (Brush)GetValue(ConnectorFillProperty); } set { SetValue(ConnectorFillProperty, value); } }
        public Brush MouseOverConnectorFill { get { return (Brush)GetValue(MouseOverConnectorFillProperty); } set { SetValue(MouseOverConnectorFillProperty, value); } }

        public TrackingDictionary<object, DependencyObject> Connectors { get { return connectors_; } }
        #endregion

        #region Notify Property Changed Callbacks
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        public void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
        
    }
}
