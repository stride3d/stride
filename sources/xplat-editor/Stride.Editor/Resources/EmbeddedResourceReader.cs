
using System.Reflection;
using Stride.Graphics;

namespace Stride.Editor.Resources
{
    internal static class EmbeddedResourceReader
    {
        public static async Task<byte[]> GetBytesAsync(string name, Assembly? source = null)
        {
            using var stream = GetStream(name, source);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            return memory.ToArray();
        }

        public static Image GetImage(string name, Assembly? source = null)
        {            
            using var stream = GetStream(name, source);
            return Image.Load(stream);
        }

        public static Stream GetStream(string name, Assembly? source = null)
        {
            source ??= Assembly.GetCallingAssembly();
            return source.GetManifestResourceStream(name) ?? throw new Exception($"Resource {name} not found in {source.GetName().Name}");
        }

        public static async Task<string> GetStringAsync(string name, Assembly? source = null)
        {
            using var stream = GetStream(name, source);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
