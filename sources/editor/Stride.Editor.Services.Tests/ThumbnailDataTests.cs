// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Storage;
using Stride.Editor.Thumbnails;
using Stride.Graphics;

namespace Stride.Editor.Services.Tests;

public sealed class ThumbnailDataTests
{
    private static readonly IDispatcherService Dispatcher = new NullDispatcherService();

    [Fact]
    public async Task AResourceThumbnailDecodesAnEmbeddedImageByItsShortName()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "StaticThumbnail.png", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        var image = Assert.IsType<Image>(data.Presenter);
        Assert.True(image.Description.Width > 0);
    }

    [Fact]
    public async Task AResourceThumbnailDecodesAnEmbeddedImageByItsManifestName()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "Stride.Editor.Services.Tests.Resources.StaticThumbnail.png", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        Assert.IsType<Image>(data.Presenter);
    }

    [Fact]
    public async Task AMissingResourceGivesNoImage()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "FolderIconAlfredo", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        Assert.Null(data.Presenter);
    }

    [Fact]
    public void TheFactoryCreatesResourceThumbnailsOfItsAssembly()
    {
        IThumbnailDataFactory factory = new ThumbnailDataFactory(typeof(ThumbnailDataTests).Assembly);

        var data = factory.CreateFromResource(ObjectId.New(), "StaticThumbnail.png");

        Assert.IsType<ResourceThumbnailData>(data);
        Assert.Same(typeof(ThumbnailDataTests).Assembly, ((ThumbnailDataFactory)factory).ResourceAssembly);
    }

    [Fact]
    public async Task ABitmapThumbnailDecodesItsStreamOnceAndFreesIt()
    {
        var png = Stride.Core.Assets.Editor.Annotations.DeclaredResources.Read(typeof(ThumbnailDataTests).Assembly, "StaticThumbnail.png")!;
        var id = ObjectId.New();
        var data = new BitmapThumbnailData(id, new MemoryStream(png));

        await data.PrepareForPresentation(Dispatcher);

        var image = Assert.IsType<Image>(data.Presenter);
        Assert.True(image.Description.Width > 0);

        // The same thumbnail id is served from the cache, even with an unreadable stream
        var cached = new BitmapThumbnailData(id, new MemoryStream());
        await cached.PrepareForPresentation(Dispatcher);
        Assert.Same(image, cached.Presenter);
    }

    [Fact]
    public async Task AnUnreadableStreamGivesNoImage()
    {
        var data = new BitmapThumbnailData(ObjectId.New(), new MemoryStream([1, 2, 3]));

        await data.PrepareForPresentation(Dispatcher);

        Assert.Null(data.Presenter);
    }
}
