// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Core.Assets.Presentation.ViewModels
{
    /// <summary>
    /// Interface for session objects.
    /// </summary>
    public interface ISessionObjectViewModel
    {
        bool IsEditable { get; }

        string Name { get; set; }

        ISessionViewModel Session { get; }

        ThumbnailData ThumbnailData { get; }

        string TypeDisplayName { get; }
    }
}
 
