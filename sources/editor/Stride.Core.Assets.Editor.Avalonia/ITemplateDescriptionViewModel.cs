// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Stride.Core.Assets.Templates;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public interface ITemplateDescriptionViewModel
    {
        string Name { get; }

        string Description { get; }

        string FullDescription { get; }

        string Group { get; }

        Guid Id { get; }

        string DefaultOutputName { get; }

        Bitmap Icon { get; }

        IEnumerable<Bitmap> Screenshots { get; }

        TemplateDescription GetTemplate();
    }
}
