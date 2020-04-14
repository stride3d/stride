// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Presentation.Controls;

namespace Xenko.DebugTools.Behaviors
{
    public class TimeBarCurrentTimeSetterBehavior : Behavior<ScaleBar>
    {
        public ProcessInfoRenderer Renderer { get; set; }

        protected override void OnAttached()
        {
            if (Renderer == null)
                // throw new InvalidOperationException("The Renderer property must be set a valid value.");
                return; // can be null at design time

            Renderer.LastFrameRender += OnRendererLastFrameRender;
        }

        protected override void OnDetaching()
        {
            Renderer.LastFrameRender -= OnRendererLastFrameRender;
        }

        private void OnRendererLastFrameRender(object sender, FrameRenderRoutedEventArgs e)
        {
            AssociatedObject.SetUnitAt(e.FrameData.EndTime, Renderer.ActualWidth);
        }
    }
}
