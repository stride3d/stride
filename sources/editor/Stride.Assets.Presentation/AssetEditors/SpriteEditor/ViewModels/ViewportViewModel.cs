// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels
{
    /// <summary>
    /// A view model that represents a viewport with a content.
    /// </summary>
    public class ViewportViewModel : DispatcherViewModel
    {
        private double viewportWidth;
        private double viewportHeight;
        private double horizontalOffset;
        private double verticalOffset;
        private double contentWidth;
        private double contentHeight;
        private double scaleFactor = 1;
  
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewportViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use with this view model.</param>
        public ViewportViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            DependentProperties.Add(nameof(ContentWidth), new[] { nameof(ActualContentWidth) });
            DependentProperties.Add(nameof(ContentHeight), new[] { nameof(ActualContentHeight) });
            DependentProperties.Add(nameof(ScaleFactor), new[] { nameof(ActualContentWidth), nameof(ActualContentHeight) });

            FitOnScreenCommand = new AnonymousCommand(ServiceProvider, FitOnScreen);
            ScaleToRealSizeCommand = new AnonymousCommand(ServiceProvider, ScaleToRealSize);
            ZoomInCommand = new AnonymousCommand(ServiceProvider, () => ZoomIn(ViewportWidth * 0.5, ViewportHeight * 0.5));
            ZoomOutCommand = new AnonymousCommand(ServiceProvider, () => ZoomOut(ViewportWidth * 0.5, ViewportHeight * 0.5));
        }

        /// <summary>
        /// Gets or sets the width of the viewport in pixel.
        /// </summary>
        public double ViewportWidth { get { return viewportWidth; } set { SetValue(ref viewportWidth, value); } }

        /// <summary>
        /// Gets or sets the height of the viewport in pixel.
        /// </summary>
        public double ViewportHeight { get { return viewportHeight; } set { SetValue(ref viewportHeight, value); } }

        /// <summary>
        /// Gets or sets the horizontal offset of the viewport in pixel.
        /// </summary>
        public double HorizontalOffset { get { return horizontalOffset; } set { SetValue(ref horizontalOffset, value); } }

        /// <summary>
        /// Gets or sets the vertical offset of the viewport in pixel.
        /// </summary>
        public double VerticalOffset { get { return verticalOffset; } set { SetValue(ref verticalOffset, value); } }

        /// <summary>
        /// Gets or sets the content width in pixel.
        /// </summary>
        public double ContentWidth { get { return contentWidth; } set { SetValue(ref contentWidth, value); } }

        /// <summary>
        /// Gets or sets the content height in pixel.
        /// </summary>
        public double ContentHeight { get { return contentHeight; } set { SetValue(ref contentHeight, value); } }

        /// <summary>
        /// Gets or sets the scale factor of the content.
        /// </summary>
        public double ScaleFactor { get { return scaleFactor; } set { SetValue(ref scaleFactor, value); } }

        /// <summary>
        /// Gets the actual content width on screen, in pixel. This property takes account of the <see cref="ScaleFactor"/>.
        /// </summary>
        public double ActualContentWidth => ContentWidth * ScaleFactor;

        /// <summary>
        /// Gets the actual content height on screen, in pixel. This property takes account of the <see cref="ScaleFactor"/>.
        /// </summary>
        public double ActualContentHeight => ContentHeight * ScaleFactor;

        /// <summary>
        /// Gets the command that will increase the zoom in the viewport.
        /// </summary>
        public CommandBase ZoomInCommand { get; private set; }

        /// <summary>
        /// Gets the command that will decrease the zoom in the viewport.
        /// </summary>
        public CommandBase ZoomOutCommand { get; private set; }

        /// <summary>
        /// Gets the command that will make the content fit on the viewport.
        /// </summary>
        public CommandBase FitOnScreenCommand { get; private set; }

        /// <summary>
        /// Gets the command that will scale the xontent to its real size in pixel.
        /// </summary>
        public CommandBase ScaleToRealSizeCommand { get; private set; }

        /// <summary>
        /// Increases the zoom level.
        /// </summary>
        /// <param name="centerPointX">The X component of the point that should stay at the same position after zooming.</param>
        /// <param name="centerPointY">The Y component of the point that should stay at the same position after zooming.</param>
        public void ZoomIn(double centerPointX, double centerPointY)
        {
            var newValue = Utils.ZoomFactors.FirstOrDefault(x => x > ScaleFactor);
            if (newValue < double.Epsilon)
                newValue = Utils.ZoomFactors.Last();

            ChangeScale(newValue, centerPointX, centerPointY);
        }

        /// <summary>
        /// Decreases the zoom level.
        /// </summary>
        /// <param name="centerPointX">The X component of the point that should stay at the same position after zooming.</param>
        /// <param name="centerPointY">The Y component of the point that should stay at the same position after zooming.</param>
        public void ZoomOut(double centerPointX, double centerPointY)
        {
            var newValue = Utils.ZoomFactors.LastOrDefault(x => x < ScaleFactor);
            if (newValue < double.Epsilon)
                newValue = Utils.ZoomFactors.First();

            ChangeScale(newValue, centerPointX, centerPointY);
        }

        /// <summary>
        /// Changes the zoom level so that the content fits on the viewport.
        /// </summary>
        public void FitOnScreen()
        {
            const double Margins = 20.0;
            var scale = Math.Min((ViewportWidth - Margins) / ContentWidth, (ViewportHeight - Margins) / ContentHeight);
            ChangeScale(scale, ViewportWidth * 0.5, ViewportHeight * 0.5);
        }

        /// <summary>
        /// Scales the content to its real size in pixel.
        /// </summary>
        public void ScaleToRealSize()
        {
            ChangeScale(1.0, ViewportWidth * 0.5, ViewportHeight * 0.5);
        }

        /// <summary>
        /// Changes the scale to the given value, keeping the given point at the same position on the viewport.
        /// </summary>
        /// <param name="newValue">The new value for the scale factor.</param>
        /// <param name="centerPointX">The X component of the point that should stay at the same position after rescaling.</param>
        /// <param name="centerPointY">The Y component of the point that should stay at the same position after rescaling.</param>
        public void ChangeScale(double newValue, double centerPointX, double centerPointY)
        {
            HorizontalOffset = newValue / ScaleFactor * (HorizontalOffset + centerPointX) - centerPointX;
            VerticalOffset = newValue / ScaleFactor * (VerticalOffset + centerPointY) - centerPointY;
            ScaleFactor = newValue;
        }
    }
}
