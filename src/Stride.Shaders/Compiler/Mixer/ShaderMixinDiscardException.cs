using System.Runtime.Serialization;

namespace Stride.Shaders.Mixer
{
    internal class ShaderMixinDiscardException : Exception
    {
        public ShaderMixinDiscardException()
        {
        }

        public ShaderMixinDiscardException(string? message) : base(message)
        {
        }

        public ShaderMixinDiscardException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected ShaderMixinDiscardException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}