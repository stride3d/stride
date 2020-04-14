// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;


[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: InternalsVisibleTo("Stride.Core.Presentation.Tests")]

[assembly: XmlnsPrefix("http://schemas.stride3d.net/xaml/presentation", "sd")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Behaviors")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Collections")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Commands")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Controls")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Controls.Commands")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Core")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.Interactivity")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.MarkupExtensions")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.ValueConverters")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.View")]
[assembly: XmlnsDefinition("http://schemas.stride3d.net/xaml/presentation", "Stride.Core.Presentation.ViewModel")]
