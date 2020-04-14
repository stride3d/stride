// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Assets.TextAccessors
{
    public class DefaultTextAccessor : ITextAccessor
    {
        private string text;

        public string FilePath { get; internal set; }

        /// <inheritdoc/>
        public string Get()
        {
            return text ?? (text = (FilePath != null ? LoadFromFile() : FilePath) ?? "");
        }

        /// <inheritdoc/>
        public void Set(string value)
        {
            text = value;
        }

        public async Task Save(Stream stream)
        {
            if (text != null)
            {
                using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                {
                    await streamWriter.WriteAsync(text);
                }
            }
            else if (FilePath != null)
            {
                using (var inputStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, useAsync: true))
                {
                    await inputStream.CopyToAsync(stream);
                }
            }
        }

        public ISerializableTextAccessor GetSerializableVersion()
        {
            // Still not loaded?
            if (text == null && FilePath != null)
                return new FileTextAccessor { FilePath = FilePath };

            return new StringTextAccessor { Text = text };
        }

        private string LoadFromFile()
        {
            if (FilePath == null)
                return null;

            try
            {
                return File.ReadAllText(FilePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
