using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

// The asset files of this plugin and the type their compiled content loads as, for the asset URL constants generator
[assembly: AssetFileExtension(".sdmytemplate", typeof(MyTemplate.MyTemplateData))]

namespace MyTemplate;

/// <summary>
/// The runtime content a MyTemplate asset compiles to. A member of this type references the content (an asset
/// picker in the editor) instead of holding a copy.
/// </summary>
[DataContract]
[ContentSerializer(typeof(DataContentSerializer<MyTemplateData>))]
[ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyTemplateData>), Profile = "Content")]
public class MyTemplateData
{
    public float Speed { get; set; }
}
