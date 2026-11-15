using Stride.Core;
using Stride.Core.Assets;

namespace MyTemplate.Assets;

/// <summary>
/// An asset of the plugin, compiled to <see cref="MyTemplateData"/> by <see cref="MyTemplateAssetCompiler"/>.
/// </summary>
[DataContract("MyTemplateAsset")]
[AssetDescription(".sdmytemplate")]
[AssetContentType(typeof(MyTemplateData))]
[AssetFormatVersion("MyTemplate", "1.0.0.0")]
[Display(100, "MyTemplate")]
public class MyTemplateAsset : Asset
{
    public float Speed { get; set; } = 2.0f;
}
