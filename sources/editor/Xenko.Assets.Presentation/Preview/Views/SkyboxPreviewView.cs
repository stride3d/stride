// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using Xenko.Editor.Preview.View;

namespace Xenko.Assets.Presentation.Preview.Views
{
    public class SkyboxPreviewView : XenkoPreviewView
    {
        static SkyboxPreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SkyboxPreviewView), new FrameworkPropertyMetadata(typeof(SkyboxPreviewView)));
        }
    }
}
