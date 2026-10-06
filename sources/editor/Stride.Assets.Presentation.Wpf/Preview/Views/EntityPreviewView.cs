// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Stride.Editor.Preview.View;

namespace Stride.Assets.Presentation.Preview.Views
{
    // DO NOT REACTIVATE THIS PREVIEW WITHOUT MAKING A DISTINCT PREVIEW BETWEEN ENTITIES AND SCENE! SCENE IS LOADED (AND NOW UNLOADED) at initialization, we absolutely don't want to do that
    //[AssetPreviewView<EntityPreview>]
    public class EntityPreviewView : StridePreviewView
    {
        static EntityPreviewView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityPreviewView), new FrameworkPropertyMetadata(typeof(EntityPreviewView)));
        }
    }
}
