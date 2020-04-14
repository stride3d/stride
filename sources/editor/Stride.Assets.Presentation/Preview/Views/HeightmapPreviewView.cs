// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Stride.Editor.Preview.View;

namespace Stride.Assets.Presentation.Preview.Views
{
    public class HeightmapPreviewView : StridePreviewView
    {
        static HeightmapPreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeightmapPreviewView), new FrameworkPropertyMetadata(typeof(HeightmapPreviewView)));
        }
    }
}
