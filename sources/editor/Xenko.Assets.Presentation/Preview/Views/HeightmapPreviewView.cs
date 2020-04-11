// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Xenko.Editor.Preview.View;

namespace Xenko.Assets.Presentation.Preview.Views
{
    public class HeightmapPreviewView : XenkoPreviewView
    {
        static HeightmapPreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeightmapPreviewView), new FrameworkPropertyMetadata(typeof(HeightmapPreviewView)));
        }
    }
}
