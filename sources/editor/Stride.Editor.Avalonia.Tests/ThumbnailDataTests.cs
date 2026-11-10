// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Avalonia.Services;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Storage;
using Stride.Core.Assets.Presentation.Services;

namespace Stride.Editor.Avalonia.Tests;

/// <summary>
/// The thumbnails of the Avalonia editor: their presenter is an Avalonia bitmap, decoded from the PNG the thumbnail service
/// built or from an image embedded in the shell.
/// </summary>
public sealed class ThumbnailDataTests
{
    private static readonly IDispatcherService Dispatcher = new NullDispatcherService();

    private static byte[] Png => DeclaredResources.Read(typeof(ThumbnailDataTests).Assembly, "StaticThumbnail.png")!;

    [AvaloniaFact]
    public async Task AResourceThumbnailDecodesAnEmbeddedImageByItsShortName()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "StaticThumbnail.png", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        var bitmap = Assert.IsType<Bitmap>(data.Presenter);
        Assert.True(bitmap.PixelSize.Width > 0);
    }

    [AvaloniaFact]
    public async Task AResourceThumbnailFindsAnEmbeddedPngWithoutItsExtension()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "StaticThumbnail", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        Assert.IsType<Bitmap>(data.Presenter);
    }

    [AvaloniaFact]
    public async Task AResourceThumbnailDecodesAnEmbeddedImageByItsManifestName()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "Stride.Editor.Avalonia.Tests.Resources.StaticThumbnail.png", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        Assert.IsType<Bitmap>(data.Presenter);
    }

    [AvaloniaFact]
    public async Task AMissingResourceGivesNoImage()
    {
        var data = new ResourceThumbnailData(ObjectId.New(), "FolderIconAlfredo", typeof(ThumbnailDataTests).Assembly);

        await data.PrepareForPresentation(Dispatcher);

        Assert.Null(data.Presenter);
    }

    [AvaloniaFact]
    public async Task TheFactoryCreatesResourceThumbnailsOfItsAssemblyAndBitmapThumbnailsOfAStream()
    {
        IThumbnailDataFactory factory = new ThumbnailDataFactory(typeof(ThumbnailDataTests).Assembly);

        var resource = factory.CreateFromResource(ObjectId.New(), "StaticThumbnail.png");
        var built = factory.CreateFromStream(ObjectId.New(), new MemoryStream(Png));

        Assert.IsType<ResourceThumbnailData>(resource);
        Assert.IsType<BitmapThumbnailData>(built);
        await built.PrepareForPresentation(Dispatcher);
        Assert.IsType<Bitmap>(built.Presenter);
        Assert.Same(typeof(ThumbnailDataTests).Assembly, ((ThumbnailDataFactory)factory).ResourceAssembly);
    }

    [AvaloniaFact]
    public async Task ABitmapThumbnailDecodesItsStreamOnceAndFreesIt()
    {
        var id = ObjectId.New();
        var stream = new MemoryStream(Png);
        var data = new BitmapThumbnailData(id, stream);

        await data.PrepareForPresentation(Dispatcher);

        var bitmap = Assert.IsType<Bitmap>(data.Presenter);
        Assert.True(bitmap.PixelSize.Width > 0);
        Assert.False(stream.CanRead);

        // The same thumbnail id is served from the cache, even with an unreadable stream
        var cached = new BitmapThumbnailData(id, new MemoryStream());
        await cached.PrepareForPresentation(Dispatcher);
        Assert.Same(bitmap, cached.Presenter);
    }

    [AvaloniaFact]
    public async Task AnUnreadableStreamGivesNoImage()
    {
        var data = new BitmapThumbnailData(ObjectId.New(), new MemoryStream([1, 2, 3]));

        await data.PrepareForPresentation(Dispatcher);

        Assert.Null(data.Presenter);
    }

    [AvaloniaFact]
    public void TheThumbnailConverterPassesBitmapsAndDropsTheRest()
    {
        using var bitmap = new Bitmap(new MemoryStream(Png));
        var converter = new Core.Assets.Editor.Avalonia.Converters.ThumbnailToImage();

        Assert.Same(bitmap, converter.Convert(bitmap, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Same(global::Avalonia.AvaloniaProperty.UnsetValue, converter.Convert("not an image", typeof(object), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Same(global::Avalonia.AvaloniaProperty.UnsetValue, converter.Convert(null, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture));
    }
}
