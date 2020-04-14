// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.TextureConverter.Requests;
using Stride.TextureConverter.TexLibraries;
using System.Runtime.CompilerServices;
using Stride.TextureConverter.Backend.Requests;

namespace Stride.TextureConverter
{

    /// <summary>
    /// Provides method to load images or textures, to modify them and to convert them with different texture compression format.
    /// Input supported format : gif, png, jpe, pds (Every FreeImage supported format...), dds, pvr, ktx.
    /// Output format : gif, png, jpe, pds (Every FreeImage supported format...), dds, pvr, ktx.
    /// Compression format : DXT1-5, ATC, PVRTC1-2, ETC1-2, uncompressed formats (BGRA8888, RGBA8888)
    /// Image processing : resize, flip, gamma correction
    /// Texture utilities : Mipmap generation, normal map generation
    /// </summary>
    public class TextureTool : IDisposable
    {

        /// <summary>
        /// The list of texture processing libraries
        /// </summary>
        private List<ITexLibrary> textureLibraries;

        private static Logger Log = GlobalLogger.GetLogger("TextureTool");
        
        static TextureTool()
        {
            var type = typeof(TextureTool);
            NativeLibrary.PreloadLibrary("DxtWrapper.dll", type);
            NativeLibrary.PreloadLibrary("PVRTexLib.dll", type);
            NativeLibrary.PreloadLibrary("PvrttWrapper.dll", type);
            NativeLibrary.PreloadLibrary("FreeImage.dll", type);
            NativeLibrary.PreloadLibrary("FreeImageNET.dll", type);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureTool"/> class.
        /// </summary>
        /// <remarks>
        /// Creating instance of each texture processing libraries. For a multithreaded use, one instance of <see cref="TextureTool"/> should be created per thread.
        /// </remarks>
        public TextureTool()
        {
            textureLibraries = new List<ITexLibrary>
            {
                new DxtTexLib(), // used to compress/decompress texture to DXT1-5 and load/save *.dds compressed texture files.
                new FITexLib(), // used to open/save common bitmap image formats.
                new StrideTexLibrary(), // used to save/load stride texture format.
                new PvrttTexLib(), // used to compress/decompress texture to PVRTC1-2 and ETC1-2 and load/save *.pvr compressed texture file.
                new ColorKeyTexLibrary(), // used to apply ColorKey on R8G8B8A8/B8G8R8A8_Unorm
                new AtlasTexLibrary(), // used to create and manipulate texture atlas
                new ArrayTexLib(), // used to create and manipulate texture array and texture cube
            };
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources for each texture porcessing libraries.
        /// </summary>
        public void Dispose()
        {
            foreach (ITexLibrary library in textureLibraries)
            {
                library.Dispose();
            }
        }


        /// <summary>
        /// Creates an atlas with the given TexImage.
        /// </summary>
        /// <param name="textureList">The texture list.</param>
        /// <param name="forceSquaredAtlas">boolean to decide wheter the atlas will be squared (default is false).</param>
        /// <returns>An instance of <see cref="TexAtlas"/>.</returns>
        /// <exception cref="TextureToolsException">No available library could create the atlas.</exception>
        public TexAtlas CreateAtlas(List<TexImage> textureList, bool forceSquaredAtlas = false)
        {
            TexAtlas atlas = new TexAtlas();
            AtlasCreationRequest request = new AtlasCreationRequest(textureList, forceSquaredAtlas);

            ITexLibrary library = FindLibrary(atlas, request);
            if (library == null)
            {
                Log.Error("No available library could create the atlas.");
                throw new TextureToolsException("No available library could create the atlas.");
            }

            atlas.Format = textureList[0].Format;
            foreach (TexImage texture in textureList)
            {
                if (texture.Format != atlas.Format)
                {
                    Log.Error("The textures in the list must all habe the same format.");
                    throw new TextureToolsException("The textures in the list must all habe the same format.");
                }
                texture.Update();
            }

            ExecuteRequest(atlas, request);

            return atlas;
        }


        /// <summary>
        /// Retrieves the atlas from a TexImage and its corresponding layout file.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="layoutFile">The layout file.</param>
        /// <returns>An instance of <see cref="TexAtlas"/>.</returns>
        /// <exception cref="TextureToolsException">The layout file doesn't exist. Please check the file path.</exception>
        public TexAtlas RetrieveAtlas(TexImage texture, string layoutFile)
        {
            if (!File.Exists(layoutFile))
            {
                Log.Error("The file " + layoutFile + " doesn't exist. Please check the file path.");
                throw new TextureToolsException("The file " + layoutFile + " doesn't exist. Please check the file path.");
            }

            return new TexAtlas(TexAtlas.TexLayout.Import(layoutFile), texture);
        }


        /// <summary>
        /// Creates a texture array with the given TexImage.
        /// </summary>
        /// <param name="textureList">The texture list.</param>
        /// <returns>An instance of <see cref="TexImage"/> corresponding containing the texture array.</returns>
        /// <exception cref="TextureToolsException">
        /// No available library could create the array.
        /// or
        /// The textures must all have the same size and format to be in a texture array.
        /// </exception>
        public TexImage CreateTextureArray(List<TexImage> textureList)
        {
            var array = new TexImage();
            var request = new ArrayCreationRequest(textureList);

            ITexLibrary library = FindLibrary(array, request);
            if (library == null)
            {
                Log.Error("No available library could create the array.");
                throw new TextureToolsException("No available library could create the array.");
            }

            int width = textureList[0].Width;
            int height = textureList[0].Height;
            int depth = textureList[0].Depth;
            array.Format = textureList[0].Format;

            foreach (var texture in textureList)
            {
                texture.Update();
                if (texture.Width != width || texture.Height != height || texture.Depth != depth || texture.Format != array.Format)
                {
                    Log.Error("The textures must all have the same size and format to be in a texture array.");
                    throw new TextureToolsException("The textures must all have the same size and format to be in a texture array.");
                }
            }
  
            ExecuteRequest(array, request);

            return array;
        }


        /// <summary>
        /// Creates a texture cube with the given TexImage.
        /// </summary>
        /// <param name="textureList">The texture list.</param>
        /// <returns>An instance of <see cref="TexImage"/> containing the texture cube.</returns>
        /// <exception cref="TextureToolsException">
        /// No available library could create the cube.
        /// or
        /// The number of texture in the texture list must be a multiple of 6.
        /// or
        /// The textures must all have the same size and format to be in a texture cube.
        /// </exception>
        public TexImage CreateTextureCube(List<TexImage> textureList)
        {
            var cube = new TexImage();
            var request = new CubeCreationRequest(textureList);

            if (textureList.Count % 6 != 0)
            {
                Log.Error("The number of texture in the texture list must be a multiple of 6.");
                throw new TextureToolsException("The number of texture in the texture list must be a multiple of 6.");
            }

            ITexLibrary library = FindLibrary(cube, request);
            if (library == null)
            {
                Log.Error("No available library could create the cube.");
                throw new TextureToolsException("No available library could create the cube.");
            }

            int width = textureList[0].Width;
            int height = textureList[0].Height;
            int depth = textureList[0].Depth;
            cube.Format = textureList[0].Format;

            foreach (var texture in textureList)
            {
                texture.Update();
                if (texture.Width != width || texture.Height != height || texture.Depth != depth || texture.Format != cube.Format)
                {
                    Log.Error("The textures must all have the same size and format to be in a texture cube.");
                    throw new TextureToolsException("The textures must all have the same size and format to be in a texture cube.");
                }
            }

            ExecuteRequest(cube, request);

            return cube;
        }


        /// <summary>
        /// Loads the Atlas corresponding to the specified layout and file.
        /// </summary>
        /// <param name="layout">The layout.</param>
        /// <param name="file">The file.</param>
        /// <returns>An instance of <see cref="TexAtlas"/>.</returns>
        /// <exception cref="TextureToolsException">
        /// The file doesn't exist. Please check the file path.
        /// or
        /// The layout doesn't match the given atlas file.
        /// </exception>
        public TexAtlas LoadAtlas(TexAtlas.TexLayout layout, string file)
        {
            if (!File.Exists(file))
            {
                Log.Error("The file " + file + " doesn't exist. Please check the file path.");
                throw new TextureToolsException("The file " + file + " doesn't exist. Please check the file path.");
            }

            var atlas = new TexAtlas(layout, Load(new LoadingRequest(file, false)));

            CheckConformity(atlas, layout);
            
            return atlas;
        }


        /// <summary>
        /// Loads the Atlas corresponding to the specified texture file and layout file.
        /// </summary>
        /// <param name="layoutFile">The layout.</param>
        /// <param name="file">The file.</param>
        /// <returns>An instance of <see cref="TexAtlas"/>.</returns>
        /// <exception cref="TextureToolsException">
        /// The file doesn't exist. Please check the file path.
        /// or
        /// The layout doesn't match the given atlas file.
        /// </exception>
        public TexAtlas LoadAtlas(string file, string layoutFile = "")
        {
            if (!File.Exists(file))
            {
                Log.Error("The file " + file + " doesn't exist. Please check the file path.");
                throw new TextureToolsException("The file " + file + " doesn't exist. Please check the file path.");
            }

            if (!layoutFile.Equals("") && !File.Exists(layoutFile))
            {
                Log.Error("The file " + layoutFile + " doesn't exist. Please check the file path.");
                throw new TextureToolsException("The file " + layoutFile + " doesn't exist. Please check the file path.");
            }
            else
            {
                layoutFile = Path.ChangeExtension(file, TexAtlas.TexLayout.Extension);
                if (!File.Exists(layoutFile))
                {
                    Log.Error("Please check that the layout file is in the same directory as the atlas, with the same name and " + TexAtlas.TexLayout.Extension + " as extension.");
                    throw new TextureToolsException("Please check that the layout file is in the same directory as the atlas, with the same name and ." + TexAtlas.TexLayout.Extension + " as extension.");
                }
            }

            var layout = TexAtlas.TexLayout.Import(layoutFile);
            var atlas = new TexAtlas(layout, Load(new LoadingRequest(file, false)));

            CheckConformity(atlas, layout);

            return atlas;
        }


        /// <summary>
        /// Checks the conformity of an atlas an its corresponding layout.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="layout">The layout.</param>
        /// <exception cref="TextureToolsException">The layout doesn't match the given atlas file.</exception>
        private void CheckConformity(TexAtlas atlas, TexAtlas.TexLayout layout)
        {
            int rightestPoint = 0;
            int lowestPoint = 0;
            foreach (var entry in layout.TexList)
            {
                if (entry.Value.UOffset + entry.Value.Width > rightestPoint) rightestPoint = entry.Value.UOffset + entry.Value.Width;
                if (entry.Value.VOffset + entry.Value.Height > lowestPoint) lowestPoint = entry.Value.VOffset + entry.Value.Height;
            }

            if (rightestPoint > atlas.Width || lowestPoint > atlas.Height)
            {
                Log.Error("The layout doesn't match the given atlas file.");
                throw new TextureToolsException("The layout doesn't match the given atlas file.");
            }
        }

        /// <summary>
        /// Loads the specified file.
        /// </summary>
        /// <remarks>
        /// The file can be an image or a texture.
        /// </remarks>
        /// <param name="file">The file.</param>
        /// <param name="isSRgb">Indicate if the input file contains sRGB data</param>
        /// <returns>An instance of <see cref="TexImage"/>.</returns>
        /// <exception cref="TextureToolsException">The file doesn't exist. Please check the file path.</exception>
        public TexImage Load(string file, bool isSRgb)
        {
            if (!File.Exists(file))
            {
                Log.Error("The file " + file + " doesn't exist. Please check the file path.");
                throw new TextureToolsException("The file " + file + " doesn't exist. Please check the file path.");
            }

            return Load(new LoadingRequest(file, isSRgb));
        }

        /// <summary>
        /// Loads the specified image of the class <see cref="Stride.Graphics.Image"/>.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="isSRgb">Indicate if the input file contains sRGB data</param>
        /// <remarks>The ownership of the provided image is not taken by the tex tool. The user has to dispose it him-self</remarks>
        /// <returns>An instance of the class <see cref="TexImage"/> containing your loaded image</returns>
        public TexImage Load(Image image, bool isSRgb)
        {
            if (image == null) throw new ArgumentNullException("image");
            return Load(new LoadingRequest(image, isSRgb));
        }

        /// <summary>
        /// Loads the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>An instance of the class <see cref="TexImage"/> containing your loaded image</returns>
        /// <exception cref="TextureToolsException">No available library could perform the task : LoadingRequest</exception>
        private TexImage Load(LoadingRequest request)
        {
            var texImage = new TexImage();
            texImage.Name = request.FilePath == null ? "" : Path.GetFileName(request.FilePath);

            foreach (ITexLibrary library in textureLibraries)
            {
                if (library.CanHandleRequest(texImage, request))
                {
                    library.Execute(texImage, request);
                    texImage.CurrentLibrary = library;
                    return texImage;
                }
            }

            Log.Error("No available library could load your texture : " + request.Type);
            throw new TextureToolsException("No available library could perform the task : " + request.Type);
        }

        /// <summary>
        /// Decompresses the specified <see cref="TexImage"/>.
        /// </summary>
        /// <param name="image">The <see cref="TexImage"/>.</param>
        /// <param name="isSRgb">Indicate is the image to decompress is an sRGB image</param>
        public void Decompress(TexImage image, bool isSRgb)
        {
            if (image.Format.IsCompressed())
            {
                ExecuteRequest(image, new DecompressingRequest(isSRgb, image.Format));
            }
        }

        public void InvertY(TexImage image)
        {
            ExecuteRequest(image, new InvertYUpdateRequest {NormalMap = image } );
        }

        /// <summary>
        /// Converts the <see cref="TexImage"/> to the specified destination pixelformat.
        /// </summary>
        /// <param name="image">The <see cref="TexImage"/>.</param>
        /// <param name="destinationFormat">The destination pixel format</param>
        public void Convert(TexImage image, PixelFormat destinationFormat)
        {
            ExecuteRequest(image, new ConvertingRequest(destinationFormat));
        }

        /// <summary>
        /// Saves the specified <see cref="TexImage"/> into a file.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="minimumMipMapSize">Minimum size of the mip map.</param>
        public void Save(TexImage image, String fileName, int minimumMipMapSize=1)
        {
            if (fileName == null || fileName.Equals(""))
            {
                Log.Error("No file name entered.");
                throw new TextureToolsException("No file name entered.");
            }

            var request = new ExportRequest(fileName, minimumMipMapSize);

            if (FindLibrary(image, request) == null && image.Format.IsCompressed())
            {
                Log.Warning("No library can export this texture with the actual compression format. We will try to decompress it first.");
                Decompress(image, image.Format.IsSRgb());
            }

            ExecuteRequest(image, request);
        }


        /// <summary>
        /// Saves the specified <see cref="TexImage"/> into a file with the specified format.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="format">The new format.</param>
        /// <param name="minimumMipMapSize">Minimum size of the mip map.</param>
        public void Save(TexImage image, String fileName, PixelFormat format, int minimumMipMapSize = 1)
        {
            if (fileName == null || fileName.Equals(""))
            {
                Log.Error("No file name entered.");
                throw new TextureToolsException("No file name entered.");
            }

            if (minimumMipMapSize < 0)
            {
                Log.Error("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
                throw new TextureToolsException("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
            }

            if (image.Format != format && format.IsCompressed() && !image.Format.IsCompressed())
            {
                TexImage workingImage = (TexImage)image.Clone();
                Compress(workingImage, format);
                ExecuteRequest(workingImage, new ExportRequest(fileName, minimumMipMapSize));
                workingImage.Dispose();
            }
            else if (image.Format != format && format.IsCompressed())
            {
                TexImage workingImage = (TexImage)image.Clone();
                Decompress(workingImage, image.Format.IsSRgb());
                Compress(workingImage, format);
                ExecuteRequest(workingImage, new ExportRequest(fileName, minimumMipMapSize));
                workingImage.Dispose();
            }
            else
            {
                ExecuteRequest(image, new ExportRequest(fileName, minimumMipMapSize));
            }
        }


        /// <summary>
        /// Switches the channel R and B.
        /// </summary>
        /// <remarks>
        /// PVR texture and ATC library can't handle BGRA order, channels B and R must be switched to get the new order RGBA. (This switch is made automatically)
        /// If the image is in a compressed format, it will be first decompressed.
        /// </remarks>
        /// <param name="image">The image.</param>
        public void SwitchChannel(TexImage image)
        {
            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't switch channels of a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            ExecuteRequest(image, new SwitchingBRChannelsRequest());
        }


        /// <summary>
        /// Compresses the specified image into the specified format.
        /// </summary>
        /// <remarks>
        /// If the image is in a compressed format, it will be first decompressed.
        /// If the compressing library doesn't support BGRA order and the current image format is in this order, the channels R and B will be switched.
        /// </remarks>
        /// <param name="image">The image.</param>
        /// <param name="format">The format.</param>
        public void Compress(TexImage image, PixelFormat format, TextureQuality quality = TextureQuality.Fast)
        {
            if (image.Format == format) return;

            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't compress an already compressed texture. It will be decompressed first..");
                Decompress(image, format.IsSRgb());
            }

            var request = new CompressingRequest(format, quality);

            ExecuteRequest(image, request);
        }

        /// <summary>
        /// Apply a color key on the image by replacing the color passed by to this method by a white transparent color (Alpha is 0).
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="colorKey">The color key.</param>
        public void ColorKey(TexImage image, Color colorKey)
        {
            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't compress an already compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            if (image.Format != PixelFormat.R8G8B8A8_UNorm && image.Format != PixelFormat.B8G8R8A8_UNorm 
                && image.Format != PixelFormat.B8G8R8A8_UNorm_SRgb && image.Format != PixelFormat.R8G8B8A8_UNorm_SRgb)
            {
                Log.Error($"ColorKey TextureConverter is only supporting R8G8B8A8_UNorm or B8G8R8A8_UNorm while Texture Format is [{image.Format}]");
                return;
            }

            var request = new ColorKeyRequest(colorKey);
            ExecuteRequest(image, request);
        }

        /// <summary>
        /// Generates the mip maps.
        /// </summary>
        /// <remarks>
        /// If the image is in a compressed format, it will be first decompressed.
        /// </remarks>
        /// <param name="image">The image.</param>
        /// <param name="filter">The filter.</param>
        public void GenerateMipMaps(TexImage image, Filter.MipMapGeneration filter)
        {
            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't generate mipmaps for a compressed texture. It will be decompressed first.");
                Decompress(image, image.Format.IsSRgb());
            }

            ExecuteRequest(image, new MipMapsGenerationRequest(filter));
        }


        /// <summary>
        /// Resizes the specified image to a fixed image size.
        /// </summary>
        /// <remarks>
        /// If the image is in a compressed format, it will be first decompressed.
        /// </remarks>
        /// <param name="image">The image.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="filter">The filter.</param>
        public void Resize(TexImage image, int width, int height, Filter.Rescaling filter)
        {
            if (width < 1 || height < 1)
            {
                Log.Error("The new size must be an integer > 0.");
                throw new TextureToolsException("The new size must be an integer > 0.");
            }

            // Texture already has the requested dimension
            if (image.Width == width && image.Height == height)
            {
                return;
            }

            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't resize a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            ExecuteRequest(image, new FixedRescalingRequest(width, height, filter));
        }


        /// <summary>
        /// Rescales the specified image with the specified factors.
        /// </summary>
        /// <remarks>
        /// The new size will be : width = width * widthFactor and height = height * heightFactor
        /// If the image is in a compressed format, it will be first decompressed.
        /// </remarks>
        /// <param name="image">The image.</param>
        /// <param name="widthFactor">The width factor.</param>
        /// <param name="heightFactor">The height factor.</param>
        /// <param name="filter">The filter.</param>
        public void Rescale(TexImage image, float widthFactor, float heightFactor, Filter.Rescaling filter)
        {
            if (widthFactor <= 0 || heightFactor <= 0)
            {
                Log.Error("The size factors must be positive floats.");
                throw new TextureToolsException("The size factors must be positive floats.");
            }

            // The texture dimension won't change.
            if (widthFactor == 1 && heightFactor ==1)
            {
                return;
            }

            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't rescale a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            ExecuteRequest(image, new FactorRescalingRequest(widthFactor, heightFactor, filter));
        }


        /// <summary>
        /// Generates the normal map.
        /// </summary>
        /// <param name="heightMap">The height map.</param>
        /// <param name="amplitude">The amplitude.</param>
        /// <returns>An instance of <see cref="TexImage"/> containig the normal map.</returns>
        public TexImage GenerateNormalMap(TexImage heightMap, float amplitude)
        {
            if (amplitude <= 0)
            {
                Log.Error("The amplitude must be a positive float.");
                throw new TextureToolsException("The amplitude must be a positive float.");
            }

            if (heightMap.Format.IsCompressed())
            {
                Log.Warning("You can't generate a normal map from a compressed height hmap. It will be decompressed first..");
                Decompress(heightMap, heightMap.Format.IsSRgb());
            }

            var request = new NormalMapGenerationRequest(amplitude);

            ExecuteRequest(heightMap, request);

            return request.NormalMap;
        }


        /// <summary>
        /// Premultiplies the alpha.
        /// </summary>
        /// <param name="image">The image.</param>
        public void PreMultiplyAlpha(TexImage image)
        {
            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't premultiply alpha on a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            ExecuteRequest(image, new PreMultiplyAlphaRequest());
        }
        
        /// <summary>
        /// Create a new image from the alpha component of a reference image.
        /// </summary>
        /// <param name="texImage">The image from which to take the alpha</param>
        /// <returns>The <see cref="TexImage"/> containing the alpha component as rgb color. Note: it is the user responsibility to dispose the returned image.</returns>
        public unsafe TexImage CreateImageFromAlphaComponent(TexImage texImage)
        {
            if (texImage.Dimension != TexImage.TextureDimension.Texture2D || texImage.Format.IsCompressed())
                throw new NotImplementedException();

            var alphaImage = (TexImage)texImage.Clone(true);

            var rowPtr = alphaImage.Data;
            for (int i = 0; i < alphaImage.Height; i++)
            {
                var pByte = (byte*)rowPtr;
                for (int x = 0; x < alphaImage.Width; x++)
                {
                    pByte[0] = pByte[3];
                    pByte[1] = pByte[3];
                    pByte[2] = pByte[3];

                    pByte += 4;
                }
                rowPtr = IntPtr.Add(rowPtr, alphaImage.RowPitch);
            }

            return alphaImage;
        }

        private enum EdgeDirection
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3,
        }

        private int RotationDirection(EdgeDirection previousDirection, EdgeDirection currentDirection)
        {
            if (currentDirection == EdgeDirection.Left && previousDirection == EdgeDirection.Bottom)
                return 1;

            if (currentDirection == EdgeDirection.Bottom && previousDirection == EdgeDirection.Left)
                return -1;

            return currentDirection - previousDirection;
        }

        /// <summary>
        /// Transform a integer encoded as Rgba to Bgra.
        /// </summary>
        /// <param name="value">The input value</param>
        /// <returns>The swapped value</returns>
        private static uint RgbaToBgra(uint value)
        {
            var b1 = (value >> 0) & 0xff;
            var b2 = (value >> 8) & 0xff;
            var b3 = (value >> 16) & 0xff;
            var b4 = (value >> 24) & 0xff;

            return b4 << 24 | b1 << 16 | b2 << 8 | b3 << 0;
        }

        /// <summary>
        /// Gets the alpha levels of the image in the provided region.
        /// </summary>
        /// <param name="texture">The texture</param>
        /// <param name="region">The region of the texture to analyze</param>
        /// <param name="tranparencyColor">The color used as transparent color. If null use standard alpha channel.</param>
        /// <param name="logger">The logger used to log information</param>
        /// <returns></returns>
        public unsafe AlphaLevels GetAlphaLevels(TexImage texture, Rectangle region, Color? tranparencyColor, ILogger logger = null)
        {
            // quick escape when it is possible to know the absence of alpha from the file itself
            var alphaDepth = texture.GetAlphaDepth();
            if (!tranparencyColor.HasValue && alphaDepth == 0)
                return AlphaLevels.NoAlpha;

            // check that we support the format
            var format = texture.Format;
            var pixelSize = format.SizeInBytes();
            if (texture.Dimension != TexImage.TextureDimension.Texture2D || !(format.IsRGBAOrder() || format.IsBGRAOrder() || pixelSize != 4))
            {
                var guessedAlphaLevel = alphaDepth > 0 ? AlphaLevels.InterpolatedAlpha : AlphaLevels.NoAlpha;
                logger?.Debug($"Unable to find alpha levels for texture type {format}. Returning default alpha level '{guessedAlphaLevel}'.");
                return guessedAlphaLevel;
            }

            // truncate the provided region in order to be sure to be in the texture
            region.Width = Math.Min(region.Width, texture.Width - region.Left);
            region.Height = Math.Min(region.Height, texture.Height- region.Top);

            var alphaLevel = AlphaLevels.NoAlpha;
            var stride = texture.RowPitch;
            var startPtr = (byte*)texture.Data + stride * region.Y + pixelSize * region.X;
            var rowPtr = startPtr;

            if (tranparencyColor.HasValue) // specific case when using a transparency color
            {
                var transparencyValue = format.IsRGBAOrder() ? tranparencyColor.Value.ToRgba() : tranparencyColor.Value.ToBgra();
                
                for (int y = 0; y < region.Height; ++y)
                {
                    var ptr = (int*)rowPtr;

                    for (int x = 0; x < region.Width; x++)
                    {
                        if (*ptr == transparencyValue)
                            return AlphaLevels.MaskAlpha;

                        ptr += 1;
                    }
                    rowPtr += stride;
                }
            }
            else // use default alpha channel
            {
                for (int y = 0; y < region.Height; ++y)
                {
                    var ptr = rowPtr+3;

                    for (int x = 0; x < region.Width; x++)
                    {
                        var value = *ptr;
                        if (value == 0)
                        {
                            if (alphaDepth == 1)
                                return AlphaLevels.MaskAlpha;

                            alphaLevel = AlphaLevels.MaskAlpha;
                        }
                        else if (value != 0xff)
                        {
                            return AlphaLevels.InterpolatedAlpha;
                        }

                        ptr += 4;
                    }
                    rowPtr += stride;
                }
            }

            return alphaLevel;
        }

        /// <summary>
        /// Pick the color under the specified pixel.
        /// </summary>
        /// <param name="texture">The texture</param>
        /// <param name="pixel">The coordinate of the pixel</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is null</exception>
        public unsafe Color PickColor(TexImage texture, Int2 pixel)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            var format = texture.Format;
            if (texture.Dimension != TexImage.TextureDimension.Texture2D || !(format.IsRGBAOrder() || format.IsBGRAOrder() || format.SizeInBytes() != 4))
                throw new NotImplementedException();

            // check that the pixel is inside the texture
            var textureRegion = new Rectangle(0, 0, texture.Width, texture.Height);
            if (!textureRegion.Contains(pixel))
                throw new ArgumentException("The provided pixel coordinate is outside of the texture");

            var ptr = (uint*)texture.Data;
            var stride = texture.RowPitch / 4;

            var pixelColorInt = ptr[stride*pixel.Y + pixel.X];
            var pixelColor = format.IsRGBAOrder() ? Color.FromRgba(pixelColorInt) : Color.FromBgra(pixelColorInt);

            return pixelColor;
        }

        /// <summary>
        /// Find the region of the texture containing the sprite under the specified pixel.
        /// </summary>
        /// <param name="texture">The texture containing the sprite</param>
        /// <param name="pixel">The coordinate of the pixel specifying the sprite</param>
        /// <param name="separatorColor">The separator color that delimit the sprites. If null the <see cref="Color.Transparent"/> color is used</param>
        /// <param name="separatorMask">The mask specifying which bits of the color should be checked. The bits are ordered as AABBGGRR.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is null</exception>
        /// <returns></returns>
        public unsafe Rectangle FindSpriteRegion(TexImage texture, Int2 pixel, Color? separatorColor = null, uint separatorMask = 0xff000000)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));

            var format = texture.Format;
            if (texture.Dimension != TexImage.TextureDimension.Texture2D || !(format.IsRGBAOrder() || format.IsBGRAOrder() || format.SizeInBytes() != 4))
                throw new NotImplementedException();

            // adjust the separator color the mask depending on the color format.
            var separator = (uint)(separatorColor ?? Color.Transparent).ToRgba();
            if (texture.Format.IsBGRAOrder())
            {
                separator = RgbaToBgra(separator);
                separatorMask = RgbaToBgra(separatorMask);
            }
            var maskedSeparator = separator & separatorMask;
            
            var ptr = (uint*)texture.Data;
            var stride = texture.RowPitch / 4;

            // check for empty region (provided pixel is not valid)
            var textureRegion = new Rectangle(0, 0, texture.Width, texture.Height);
            if (!textureRegion.Contains(pixel) || (ptr[pixel.Y * stride + pixel.X] & separatorMask) == maskedSeparator)
                return new Rectangle(pixel.X, pixel.Y, 0, 0);

            // initialize the region with the provided pixel
            var region = new Rectangle(pixel.X, pixel.Y, 1, 1);

            var nextSearchOffsets = new[,]
            {
                { new Int2(-1, -1),  new Int2( 0, -1) },
                { new Int2( 1, -1),  new Int2( 1,  0) },
                { new Int2( 1,  1),  new Int2( 0,  1) },
                { new Int2(-1,  1),  new Int2(-1,  0) }
            };

            var contourLeftEgde = pixel;
            var rotationDirection = 0;
            do
            {
                // Stage 1: Find an edge of the shape (look to the left of the provided pixel as long as possible)
                var startEdge = contourLeftEgde;
                var startEdgeDirection = EdgeDirection.Left;
                for (int x = startEdge.X; x >= 0; --x)
                {
                    if ((ptr[startEdge.Y * stride + x] & separatorMask) == maskedSeparator)
                        break;

                    startEdge.X = x;
                }

                // Stage 2: Determine the whole contour of the shape and update the region. 
                // Note: the found contour can correspond to an internal hole contour or the external shape contour.
                var currentEdge = startEdge;
                var currentEdgeDirection = startEdgeDirection;
                do
                {
                    var previousEdgeDirection = currentEdgeDirection;

                    var diagonalPixel = currentEdge + nextSearchOffsets[(int)currentEdgeDirection, 0];
                    var diagonalIsSeparator = !textureRegion.Contains(diagonalPixel) || (ptr[diagonalPixel.Y * stride + diagonalPixel.X] & separatorMask) == maskedSeparator;
                    var neighbourPixel = currentEdge + nextSearchOffsets[(int)currentEdgeDirection, 1];
                    var neighbourIsSeparator = !textureRegion.Contains(neighbourPixel) || (ptr[neighbourPixel.Y * stride + neighbourPixel.X] & separatorMask) == maskedSeparator;

                    // determine the next edge position
                    if (!diagonalIsSeparator)
                    {
                        currentEdge = diagonalPixel;
                        currentEdgeDirection = (EdgeDirection)(((int)currentEdgeDirection + 3) % 4);
                    }
                    else if (!neighbourIsSeparator)
                    {
                        currentEdge = neighbourPixel;
                    }
                    else
                    {
                        currentEdgeDirection = (EdgeDirection)(((int)currentEdgeDirection + 1) % 4);
                    }

                    // keep record of the point of the edge which is 
                    if (currentEdge.X < contourLeftEgde.X)
                        contourLeftEgde = currentEdge;

                    // increase or decrease the rotation counter based on the sequence of edge direction
                    rotationDirection += RotationDirection(previousEdgeDirection, currentEdgeDirection);

                    // update the rectangle
                    region = Rectangle.Union(region, currentEdge);
                }
                while (currentEdge != startEdge || currentEdgeDirection != startEdgeDirection); // as long as we do not close the contour continue to explore
                
            } // repeat the process as long as the edge found is not the shape external contour.
            while (rotationDirection != 4);

            return region;
        }

        /// <summary>
        /// Create a new image from region.
        /// </summary>
        /// <param name="texImage">The original image from which to extract the region</param>
        /// <param name="region">The region from the original image to extract.</param>
        /// <returns>The extracted region <see cref="TexImage"/>. Note: it is the user responsibility to dispose the returned image.</returns>
        public unsafe Image CreateImageFromRegion(TexImage texImage, Rectangle region)
        {
            if (texImage.Dimension != TexImage.TextureDimension.Texture2D || texImage.Format.IsCompressed())
                throw new NotImplementedException();

            Log.Info("Extracting region and exporting to Stride Image ...");

            // clamp the provided region to be sure it fits in provided image
            region.X = Math.Max(0, Math.Min(region.X, texImage.Width));
            region.Y = Math.Max(0, Math.Min(region.Y, texImage.Height));
            region.Width = Math.Max(0, Math.Min(region.Width, texImage.Width - region.X));
            region.Height = Math.Max(0, Math.Min(region.Height, texImage.Height - region.Y));

            // create the stride image
            var sdImage = Image.New2D(region.Width, region.Height, 1, texImage.Format);
            if (sdImage == null)
            {
                Log.Error("Image could not be created.");
                throw new InvalidOperationException("Image could not be created.");
            }

            // get the row pitch of the stride image
            var pixelBuffer = sdImage.GetPixelBuffer(0, 0);
            var dstRowPitch = pixelBuffer.RowStride;

            // copy the data
            if (texImage.ArraySize > 0)
            {
                var rowSrcPtr = texImage.SubImageArray[0].Data;
                var rowDstPtr = sdImage.DataPointer;
                rowSrcPtr = IntPtr.Add(rowSrcPtr, region.Y * texImage.RowPitch);
                for (int i = 0; i < region.Height; i++)
                {
                    var pSrc = ((UInt32*)rowSrcPtr) + region.X;
                    var pDst = (UInt32*)rowDstPtr;

                    for (int x = 0; x < region.Width; x++)
                        *(pDst++) = *(pSrc++);

                    rowSrcPtr = IntPtr.Add(rowSrcPtr, texImage.RowPitch);
                    rowDstPtr = IntPtr.Add(rowDstPtr, dstRowPitch);
                }
            }

            return sdImage;
        }


        /// <summary>
        /// Converts to stride image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns>The converted Stride <see cref="Stride.Graphics.Image"/>.</returns>
        /// <remarks>The user is the owner of the returned image, and has to dispose it after he finishes using it</remarks>
        public Stride.Graphics.Image ConvertToStrideImage(TexImage image)
        {
            var request = new ExportToStrideRequest();

            ExecuteRequest(image, request);

            return request.XkImage;
        }


        /// <summary>
        /// Corrects the gamma.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="gamma">The gamma.</param>
        public void CorrectGamma(TexImage image, double gamma)
        {
            if (gamma <= 0)
            {
                Log.Error("The gamma must be a positive float.");
                throw new TextureToolsException("The gamma must be a positive float.");
            }

            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't correct gamme on a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            var request = new GammaCorrectionRequest(gamma);

            ExecuteRequest(image, request);
        }


        /// <summary>
        /// Flips the specified image horizontally or vertically.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="orientation">The orientation <see cref="Orientation.Flip"/>.</param>
        public void Flip(TexImage image, Orientation orientation)
        {
            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't flip a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            var request = new FlippingRequest(orientation);

            ExecuteRequest(image, request);
        }

        /// <summary>
        /// Flips the specified image horizontally or vertically.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="index">The index of the sub-image.</param>
        /// <param name="orientation">The orientation <see cref="Orientation.Flip"/>.</param>
        public void FlipSub(TexImage image, int index, Orientation orientation)
        {
            if (image.Format.IsCompressed())
            {
                Log.Warning("You can't flip a compressed texture. It will be decompressed first..");
                Decompress(image, image.Format.IsSRgb());
            }

            var request = new FlippingSubRequest(index, orientation);

            ExecuteRequest(image, request);
        }


        /// <summary>
        /// Swaps two slices of a texture array.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="firstIndex">The index of the first sub-image.</param>
        /// <param name="secondIndex">The index of the second sub-image</param>
        public void Swap(TexImage image, int firstIndex, int secondIndex)
        {
            var request = new SwappingRequest(firstIndex, secondIndex);

            ExecuteRequest(image, request);
        }

        /// <summary>
        /// Finds a suitable library to handle the request.
        /// </summary>
        /// <param name="format">The format of the image to be processed.</param>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="ITexLibrary"/> which can handle the request or null if none could.</returns>
        private ITexLibrary FindLibrary(PixelFormat format, IRequest request)
        {
            foreach (var library in textureLibraries)
            {
                if (library.CanHandleRequest(format, request))
                {
                    return library;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a suitable library to handle the request.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="request">The request.</param>
        /// <returns>The <see cref="ITexLibrary"/> which can handle the request or null if none could.</returns>
        private ITexLibrary FindLibrary(TexImage image, IRequest request)
        {
            foreach (var library in textureLibraries)
            {
                if (library.CanHandleRequest(image, request))
                {
                    return library;
                }
            }

            return null;
        }


        /// <summary>
        /// Extracts the TexImage corresponding to the specified name in the given atlas.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="name">The texture name.</param>
        /// <param name="minimumMipmapSize">The minimum size of the smallest mipmap.</param>
        /// <returns>The TexImage texture corresponding to the specified name</returns>
        /// <exception cref="TextureToolsException">
        /// You must enter a texture name to extract.
        /// or
        /// The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.
        /// </exception>
        public TexImage Extract(TexAtlas atlas, string name, int minimumMipmapSize = 1)
        {
            if (name == null || name.Equals(""))
            {
                Log.Error("You must enter a texture name to extract.");
                throw new TextureToolsException("You must enter a texture name to extract.");
            }

            if (minimumMipmapSize < 0)
            {
                Log.Error("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
                throw new TextureToolsException("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
            }

            var request = new AtlasExtractionRequest(name, minimumMipmapSize);

            if (atlas.Format.IsCompressed())
            {
                Decompress(atlas, atlas.Format.IsSRgb());
            }

            ExecuteRequest(atlas, request);

            return request.Texture;
        }


        /// <summary>
        /// Extracts the TexImage corresponding to the specified indice in the given texture array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="indice">The indice.</param>
        /// <param name="minimumMipmapSize">The minimum size of the smallest mipmap.</param>
        /// <returns>The TexImage texture corresponding to the specified indice</returns>
        /// <exception cref="TextureToolsException">
        /// The indice you entered is not valid.
        /// or
        /// The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.
        /// </exception>
        public TexImage Extract(TexImage array, int indice, int minimumMipmapSize = 1)
        {
            if (indice < 0 || indice > array.ArraySize-1)
            {
                Log.Error("The indice you entered is not valid.");
                throw new TextureToolsException("The indice you entered is not valid.");
            }

            if (minimumMipmapSize < 0)
            {
                Log.Error("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
                throw new TextureToolsException("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
            }

            var request = new ArrayExtractionRequest(indice, minimumMipmapSize);

            ExecuteRequest(array, request);

            return request.Texture;
        }


        /// <summary>
        /// Extracts every TexImage included in the atlas.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="minimumMipmapSize">The minimum size of the smallest mipmap.</param>
        /// <returns>The list of TexImage corresponding to each texture composing the atlas.</returns>
        /// <exception cref="TextureToolsException">The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.</exception>
        public List<TexImage> ExtractAll(TexAtlas atlas, int minimumMipmapSize = 1)
        {
            if (minimumMipmapSize < 0)
            {
                Log.Error("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
                throw new TextureToolsException("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
            }

            var request = new AtlasExtractionRequest(minimumMipmapSize);

            if (atlas.Format.IsCompressed())
            {
                Log.Warning("You can't extract a texture from a compressed atlas. The atlas will be decompressed first..");
                Decompress(atlas, atlas.Format.IsSRgb());
            }

            ExecuteRequest(atlas, request);

            return request.Textures;
        }


        /// <summary>
        /// Extracts every TexImage included in the array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="minimumMipmapSize">The minimum size of the smallest mipmap.</param>
        /// <returns>The list of TexImage corresponding to each element of the texture array.</returns>
        /// <exception cref="TextureToolsException">The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.</exception>
        public List<TexImage> ExtractAll(TexImage array, int minimumMipmapSize = 1)
        {
            if (minimumMipmapSize < 0)
            {
                Log.Error("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
                throw new TextureToolsException("The minimup Mipmap size can't be negative. Put 0 or 1 for a complete Mipmap chain.");
            }

            var request = new ArrayExtractionRequest(minimumMipmapSize);

            if (array.Format.IsCompressed())
            {
                Decompress(array, array.Format.IsSRgb());
            }

            ExecuteRequest(array, request);

            return request.Textures;
        }


        /// <summary>
        /// Updates a specific texture in the atlas with the given TexImage.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="name">The name of the texture to update (takes the TexImage's name if none given).</param>
        /// <exception cref="TextureToolsException">
        /// You must either set the Name attribute of the TexImage, or you must give the name of the texture to update in the atlas.
        /// or
        /// The new texture can't be a texture array.
        /// </exception>
        public void Update(TexAtlas atlas, TexImage texture, string name = "")
        {
            texture.Update();

            if (texture.Name.Equals("") && name.Equals(""))
            {
                Log.Error("You must either set the Name attribute of the TexImage, or you must give the name of the texture to update in the atlas.");
                throw new TextureToolsException("You must either set the Name attribute of the TexImage, or you must give the name of the texture to update in the atlas.");
            }

            if (texture.ArraySize > 1)
            {
                Log.Error("The new texture can't be a texture array.");
                throw new TextureToolsException("The new texture can't be a texture array.");
            }

            CheckConformity(atlas, texture);

            name = name.Equals("") ? texture.Name : name;

            ExecuteRequest(atlas, new AtlasUpdateRequest(texture, name));
        }


        /// <summary>
        /// Updates a specific texture in the texture array with the given TexImage.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="indice">The indice.</param>
        /// <exception cref="TextureToolsException">
        /// The first given texture must be an array texture.
        /// or
        /// The given indice is out of range in the array texture.
        /// </exception>
        public void Update(TexImage array, TexImage texture, int indice)
        {
            texture.Update();

            if (array.ArraySize == 1)
            {
                Log.Error("The first given texture must be an array texture.");
                throw new TextureToolsException("The first given texture must be an array texture.");
            }

            if (array.ArraySize-1 < indice)
            {
                Log.Error("The given indice is out of range in the array texture.");
                throw new TextureToolsException("The given indice is out of range in the array texture.");
            }

            CheckConformity(array, texture);

            ExecuteRequest(array, new ArrayUpdateRequest(texture, indice));
        }


        /// <summary>
        /// Inserts a texture into a texture array at a specified position.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="texture">The texture to be added.</param>
        /// <param name="indice">The indice.</param>
        /// <exception cref="TextureToolsException">The given indice must be between 0 and the array size</exception>
        public void Insert(TexImage array, TexImage texture, int indice)
        {
            texture.Update();

            if (indice < 0 || indice > array.ArraySize)
            {
                Log.Error("The given indice must be between 0 and " + array.ArraySize);
                throw new TextureToolsException("The given indice must be between 0 and " + array.ArraySize);
            }

            CheckConformity(array, texture);

            ExecuteRequest(array, new ArrayInsertionRequest(texture, indice));
        }


        /// <summary>
        /// Removes the texture at a specified position from a texture array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="indice">The indice.</param>
        /// <exception cref="TextureToolsException">
        /// The array size must be > 1.
        /// or
        /// The given indice must be between 0 and  + array.ArraySize
        /// </exception>
        public void Remove(TexImage array, int indice)
        {
            if (array.ArraySize == 1)
            {
                Log.Error("The array size must be > 1.");
                throw new TextureToolsException("The array size must be > 1.");
            }

            if (indice < 0 || indice > array.ArraySize-1)
            {
                Log.Error("The given indice must be between 0 and " + array.ArraySize);
                throw new TextureToolsException("The given indice must be between 0 and " + array.ArraySize);
            }

            ExecuteRequest(array, new ArrayElementRemovalRequest(indice));
        }


        /// <summary>
        /// Checks the conformity of a candidate texture with a model one : check the mipmap count and the format.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="candidate">The candidate.</param>
        private void CheckConformity(TexImage model, TexImage candidate)
        {
            if (model.MipmapCount > 1 && candidate.MipmapCount == 1)
            {
                Log.Warning("The given texture has no mipmaps. They will be generated..");
                GenerateMipMaps(candidate, Filter.MipMapGeneration.Box);
            }

            if (candidate.Format != model.Format)
            {
                Log.Warning("The given texture format isn't correct. The texture will be converted..");
                if (model.Format.IsCompressed())
                {
                    if (candidate.Format.IsCompressed()) Decompress(candidate, candidate.Format.IsSRgb());
                    Compress(candidate, model.Format);
                }
                else
                {
                    Decompress(candidate, candidate.Format.IsSRgb());
                    if (candidate.Format != model.Format) Compress(candidate, model.Format);
                }
            }
        }


        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TextureToolsException">No available library could perform the task :  + request.Type</exception>
        private void ExecuteRequest(TexImage image, IRequest request)
        {
            // First Check if the current library can handle the request
            if (image.CurrentLibrary != null && image.CurrentLibrary.CanHandleRequest(image, request))
            {
                image.CurrentLibrary.Execute(image, request);
            }
            else // Otherwise, it finds another library which can handle the request
            {
                ITexLibrary library;
                if ((library = FindLibrary(image, request)) != null)
                {
                    if (image.Format.IsBGRAOrder() && !library.SupportBGRAOrder())
                    {
                        SwitchChannel(image);
                    }

                    if (image.CurrentLibrary != null) image.CurrentLibrary.EndLibrary(image); // Ending the use of the previous library (mainly to free memory)

                    library.StartLibrary(image); // Preparing the new library : converting TexImage format to the library native format

                    library.Execute(image, request);

                    image.CurrentLibrary = library;
                }
                else // If no library could be found, an exception is thrown
                {
                    // No library was found, attempt to execute the request using a 2-step cast
                    ITexLibrary libraryOne = null;
                    ITexLibrary libraryTwo = null;
                    IRequest intermediateRequest = null;

                    switch (request.Type)
                    {
                        case RequestType.Compressing:
                            {
                                var intermediateFormat = PixelFormat.R8G8B8A8_UNorm;
                                intermediateRequest = new ConvertingRequest(intermediateFormat);
                                libraryOne = FindLibrary(image, intermediateRequest);
                                libraryTwo = FindLibrary(intermediateFormat, request);

                                Log.Verbose("Using a 2-step conversion: " + image.Format + " -> " + intermediateFormat + " -> " + ((CompressingRequest)request).Format + " ...");
                            }
                            break;

                        default:
                            break;
                    }

                    // One or both libraries were not found, cannot proceed with the request
                    if (libraryOne == null || libraryTwo == null)
                    {
                        Log.Error("No available library could perform the task : " + request.Type);
                        throw new TextureToolsException("No available library could perform the task : " + request.Type);
                    }

                    // Both libraries for intermediate processing were found, preceeding with the request
                    if (image.Format.IsBGRAOrder() && !library.SupportBGRAOrder())
                    {
                        SwitchChannel(image);
                    }

                    if (image.CurrentLibrary != null) image.CurrentLibrary.EndLibrary(image); // Ending the use of the previous library (mainly to free memory)

                    libraryOne.StartLibrary(image); // Preparing the new library : converting TexImage format to the library native format

                    libraryOne.Execute(image, intermediateRequest);

                    libraryOne.EndLibrary(image); // Ending the use of the previous library (mainly to free memory)

                    libraryTwo.StartLibrary(image); // Preparing the new library : converting TexImage format to the library native format

                    libraryTwo.Execute(image, request);

                    image.CurrentLibrary = libraryTwo;
                }
            }
        }

        static void Main(string[] args)
        {
            var texTool = new TextureTool();
            GlobalLogger.GlobalMessageLogged += new ConsoleLogListener();


            try
            {
                /*var list = new List<TexImage>();
                for (int i = 0; i < 3; ++i)
                {
                    list.Add(texTool.Load(@"C:\dev\data\test\atlas\stones256.png"));
                    list.Add(texTool.Load(@"C:\dev\data\test\atlas\square256.png"));
                }

                var cube = texTool.CreateTextureCube(list);
                //texTool.Compress(cube, Stride.Framework.Graphics.PixelFormat.BC3_UNorm);
                texTool.GenerateMipMaps(cube, Filter.MipMapGeneration.Box);

                texTool.Save(cube, @"C:\dev\data\test\cube.pvr");

                /*texTool.Remove(cube, 0);

                texTool.Save(array, @"C:\dev\data\test\array_after.dds");

                foreach (var texture in list)  
                {
                    texture.Dispose();
                }

                cube.Dispose();*/


                /*var list = new List<TexImage>();
                for (int i = 0; i < 5; ++i)
                {
                    list.Add(texTool.Load(@"C:\dev\data\test\input\atlas\stones256.png"));
                    list.Add(texTool.Load(@"C:\dev\data\test\input\atlas\square256.png"));
                }

                var array = texTool.CreateTextureArray(list);
                texTool.Compress(array, Stride.Framework.Graphics.PixelFormat.BC3_UNorm);
                //texTool.GenerateMipMaps(array, Filter.MipMapGeneration.Box);

                texTool.Save(array, @"C:\dev\data\test\array_before.dds");

                texTool.Remove(array, 0);

                texTool.Save(array, @"C:\dev\data\test\array_after.dds");

                foreach (var texture in list)
                {
                    texture.Dispose();
                }

                array.Dispose();*/


                /*var list = new List<TexImage>();
                for (int i = 0; i < 5; ++i)
                {
                    list.Add(texTool.Load(@"C:\dev\data\test\atlas\stones256.png"));
                    list.Add(texTool.Load(@"C:\dev\data\test\atlas\square256.png"));
                }

                var array = texTool.CreateTextureArray(list);
                texTool.GenerateMipMaps(array, Filter.MipMapGeneration.Box);

                texTool.Save(array, @"C:\dev\data\test\array_before.dds");

                var newImg = texTool.Load(@"C:\dev\data\test\atlas\square512.png");
                texTool.Resize(newImg, array.Width, array.Height, Filter.Rescaling.Bilinear);
                texTool.Insert(array, newImg, 1);

                var newImg = texTool.Extract(array, 2, 16);
                texTool.Save(newImg, @"C:\dev\data\test\extract.png");

                //texTool.Save(array, @"C:\dev\data\test\array_after.dds");

                foreach (var texture in list)
                {
                    texture.Dispose();
                }

                array.Dispose();
                newImg.Dispose();*/

                /*string[] fileList = Directory.GetFiles(@"C:\dev\data\test\input\atlas");
                var list = new List<TexImage>(fileList.Length);

                foreach(string filePath in fileList)
                {
                    list.Add(texTool.Load(filePath));
                }

                var atlas = texTool.CreateAtlas(list);

                texTool.Save(atlas, @"C:\dev\data\test\input\atlas_WOMipMaps.png");*/

                /*string[] fileList = Directory.GetFiles(@"C:\dev\data\test\atlas");
                var list = new List<TexImage>(fileList.Length);

                /*foreach(string filePath in fileList)
                {
                    var img = texTool.Load(filePath);
                    list.Add(img);
                    texTool.GenerateMipMaps(img, Filter.MipMapGeneration.Cubic);
                    texTool.Save(img, @"C:\dev\data\test\"+img.Name);
                }

                var img = texTool.Load(@"C:\dev\data\test\atlas\rect100_128.png");
                list.Add(img);
                texTool.GenerateMipMaps(img, Filter.MipMapGeneration.Cubic);
                texTool.Save(img, @"C:\dev\data\test\" + img.Name);

                var atlas = texTool.CreateAtlas(list);
                //texTool.GenerateMipMaps(atlas, Filter.MipMapGeneration.Box);

                /*var newImg = texTool.Extract(atlas, "rect100_128.png");
                texTool.Save(newImg, @"C:\dev\data\test\extracted.png");

                texTool.Save(atlas, @"C:\dev\data\test\atlas_with_mipmaps_already.dds");

                /*var texImage = texTool.Extract(atlas, "stones256.png");

                texTool.Update(atlas, texImage, "square256.png");

                //var newImg = texTool.Load(@"C:\dev\data\test\atlas\stones256.png");
                //var newImg = texTool.Extract(atlas, "square128.png", 16);
                //texTool.Save(newImg, @"C:\dev\data\test\extracted.png");

                texTool.Save(atlas, @"C:\dev\data\test\atlas_after.dds");

                //newImg.Dispose();
                atlas.Dispose();*/
            }
            catch (TextureToolsException)
            {
            }

            texTool.Dispose();

            Log.Info("Done.");
            Console.ReadKey();
        }
    }
}
