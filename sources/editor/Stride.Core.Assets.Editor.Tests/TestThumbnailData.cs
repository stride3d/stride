// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Presentation.Services;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Common.Tests;

public class TestThumbnailData
{
    [Fact]
    public async Task PreparedThumbnailPresentsItsImage()
    {
        var image = new object();
        var thumbnail = new FakeThumbnailData(ObjectId.New(), image);

        await thumbnail.PrepareForPresentation(new NullDispatcherService());

        Assert.Same(image, thumbnail.Presenter);
        Assert.Equal(1, thumbnail.FreeCount);
    }
}
