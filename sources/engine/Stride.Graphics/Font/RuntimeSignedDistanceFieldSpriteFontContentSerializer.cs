using Stride.Core.Serialization.Contents;

namespace Stride.Graphics.Font
{
    internal sealed class RuntimeSignedDistanceFieldSpriteFontContentSerializer : DataContentSerializer<RuntimeSignedDistanceFieldSpriteFont>
    {
        public override object Construct(ContentSerializerContext context)
        {
            return new RuntimeSignedDistanceFieldSpriteFont();
        }
    }
}
