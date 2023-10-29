// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Preview;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Annotations;

namespace Stride.Assets.Editor.ViewModels.Preview;

[AssetPreviewViewModel<EntityPreview>]
public sealed class EntityPreviewViewModel : AssetPreviewViewModel<EntityPreview>
{
    public EntityPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
    }

    protected override void OnAttachPreview(EntityPreview preview)
    {
        // Nothing for now
    }
}
