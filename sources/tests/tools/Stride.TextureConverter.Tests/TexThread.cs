// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

namespace Stride.TextureConverter.Tests
{
    class TexThread : IDisposable
    {
        private string[] fileList;
        private int num;
        private TextureTool texTool;

        public TexThread(string[] fileList, int num)
        {
            this.fileList = fileList;
            texTool = new TextureTool();
            this.num = num;
        }

        public void Dispose()
        {
            texTool.Dispose();
        }

        public void Process()
        {
            TexImage image;

            foreach(string filePath in fileList)
            {
                Console.WriteLine(@"\n Thread # " + num + @" ---------------------------------------- PROCESSING " + filePath);

                image = texTool.Load(filePath);

                texTool.Rescale(image, 0.5f, 0.5f, Filter.Rescaling.Bicubic);

                if (image.MipmapCount <= 1)
                {
                    texTool.GenerateMipMaps(image, Filter.MipMapGeneration.Cubic);
                }

                string outFile = Path.GetDirectoryName(filePath) + "\\out\\" + Path.GetFileName(filePath);
                outFile = Path.ChangeExtension(outFile, ".dds");

                texTool.Save(image, outFile, Stride.Graphics.PixelFormat.BC3_UNorm);

                image.Dispose();
            }
        }
    }
}
