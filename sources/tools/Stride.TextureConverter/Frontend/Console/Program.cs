// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Xenko;
using Xenko.Graphics;
using Xenko.Core.Diagnostics;


namespace Xenko.TextureConverter
{
    class Program : ConsoleProgram
    {
        [Option("InputPatternFile", Required = true)]
        public string InputPatternFile = null;

        [Option("OutputFileOrDirectory", Required = true)]
        public string OutputFileOrDirectory;

        [Option("Exclude:", Description = "Exclude a particular file - partial name -", Value = "<filename>")]
        public List<string> ExcludeList = new List<string>();

        [Option("Width:", Description = "Width in pixels or percentage (default is 100p)", Value = "<width>")]
        public string Width = null;

        [Option("Height:", Description = "Height in pixels or percentage (default is 100p)", Value = "<height>")]
        public string Height = null;

        [Option("SizeFilter:", Description = "Filter used to resize the texture (default is Bilinear)", Value = "<rescalingfilter>")]
        public Filter.Rescaling RescalingFilter = Filter.Rescaling.Bilinear;

        [Option("Normal", Description = "Specify that the input is a normal map (default is false)")]
        public bool Normal = false;

        [Option("Format:", Description = "Output Texture Format default (default is the same as the input texture)", Value = "<format>")]
        public PixelFormat? TextureFormat = null;

        [Option("MipMap", Description = "Specify that mip map we be generated (default is false)", Value = "<mipmap>")]
        public bool MipMap = false;

        [Option("MipMapFilter:", Description = "Filter used to generate the mipmaps (default is Linear)", Value = "<mipmapfilter>")]
        public Filter.MipMapGeneration MipMapFilter = Filter.MipMapGeneration.Linear;

        [Option("MipMapSize:", Description = "The size (width or height) of the smallest mipmap. (default is 1)", Value = "<mipmapsize>")]
        public int MipMapSize = 1;

        [Option("PreMulAlpha", Description = "Specify that the alpha must be premultiplied", Value = "<premulalpha>")]
        public bool PreMulAlpha = false;

        [Option("Flip:", Description = "Flip the texture in the specified orientation", Value = "<flip>")]
        public Orientation? FlipOrientation = null;

        [Option("IsSRgb", Description = "Specify that the input file is an sRGB file (default is false)", Value = "<issrgb>")]
        public bool IsSRgb = false;
        
        /*public static void Main(string[] args)
        {
            new Program().Run(args);

            Console.ReadLine();
        }*/


        public override string GetUsageFooter()
        {
            var supportedFormat = new StringBuilder();
            supportedFormat.AppendLine();
            supportedFormat.AppendFormat("Supported format: {0}", string.Join(", ", Enum.GetNames(typeof(PixelFormat))));
            return supportedFormat.ToString();
        }

        public void Run(string[] args)
        {
            // Print the exe header
            PrintHeader();

            foreach (String s in args)
                Console.WriteLine(s);

            Console.WriteLine("");

            // Parse the command line
            if (!ParseCommandLine(args))
            {
                Environment.Exit(-1);
            }

            // Check if we have a pattern
            InputPatternFile = Path.Combine(Environment.CurrentDirectory, InputPatternFile);

            int indexOfPattern = InputPatternFile.IndexOf('*');
            bool isPattern = indexOfPattern >= 0;

            if (!isPattern)
            {
                InputPatternFile = Path.GetFullPath(InputPatternFile);
            }
            OutputFileOrDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, OutputFileOrDirectory));

            var inputOutputFiles = new List<Tuple<string, string>>();

            if (isPattern)
            {

                if (!Directory.Exists(OutputFileOrDirectory))
                {
                    Directory.CreateDirectory(OutputFileOrDirectory);
                }

                var directory = InputPatternFile.Substring(0, indexOfPattern);
                var pattern = InputPatternFile.Substring(indexOfPattern, InputPatternFile.Length - indexOfPattern);

                foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
                {
                    var outputFile = Path.Combine(OutputFileOrDirectory, file.Substring(directory.Length, file.Length - directory.Length));

                    bool excludeFile = false;
                    foreach (var excludeItem in ExcludeList)
                    {
                        if (file.IndexOf(excludeItem, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            excludeFile = true;
                            break;
                        }
                    }

                    if (!excludeFile)
                    {
                        inputOutputFiles.Add(new Tuple<string, string>(file, outputFile));
                    }
                }
            }
            else
            {
                inputOutputFiles.Add(new Tuple<string, string>(InputPatternFile, OutputFileOrDirectory));
            }

            var texTool = new TextureTool();
            GlobalLogger.GlobalMessageLogged += new ConsoleLogListener();

            bool hasErrors = false;
            foreach (var inputOutputFile in inputOutputFiles)
            {
                var inputFile = inputOutputFile.Item1;
                var outputFile = inputOutputFile.Item2;

                TexImage image = null;
                try
                {
                    image = texTool.Load(inputFile, IsSRgb);

                    HandleResizing(texTool, image);

                    if (FlipOrientation.HasValue) texTool.Flip(image, FlipOrientation.Value);

                    if (MipMap) texTool.GenerateMipMaps(image, MipMapFilter);
                    
                    if (PreMulAlpha) texTool.PreMultiplyAlpha(image);

                    if (TextureFormat.HasValue) texTool.Compress(image, TextureFormat.Value);

                    texTool.Save(image, outputFile, MipMapSize);
                }
                catch (TextureToolsException)
                {
                    hasErrors = true;
                }
                finally
                {
                    if (image!=null) image.Dispose();
                }
            }

            texTool.Dispose();

            if (hasErrors)
            {
                //Environment.Exit(-1);
            }
        }

        private void HandleResizing(TextureTool texTool, TexImage image)
        {
            if (Width != null && Height != null)
            {
                bool targetInPercent;
                var width = ParsePixelSize(Width, out targetInPercent);
                var height = ParsePixelSize(Height, out targetInPercent);

                if (targetInPercent)
                    texTool.Rescale(image, width / 100f, height / 100f, RescalingFilter);
                else
                    texTool.Resize(image, width, height, RescalingFilter);
            }
            else if (Width != null && Height == null)
            {
                bool targetInPercent;
                var width = ParsePixelSize(Width, out targetInPercent);

                if (targetInPercent)
                    texTool.Rescale(image, width / 100f, 1, RescalingFilter);
                else
                    texTool.Resize(image, width, image.Height, RescalingFilter);
            }
            else if (Width == null && Height != null)
            {
                bool targetInPercent;
                var height = ParsePixelSize(Height, out targetInPercent);

                if (targetInPercent)
                    texTool.Rescale(image, 1, height / 100f, RescalingFilter);
                else
                    texTool.Resize(image, image.Width, height, RescalingFilter);
            }
        }

        private int ParsePixelSize(string pixelSize, out bool isPercentage)
        {
            isPercentage = false;
            if (pixelSize.EndsWith("p"))
            {
                pixelSize = pixelSize.TrimEnd('p');
                isPercentage = true;
            }

            return int.Parse(pixelSize);
        }
    }
}
