// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Stride.TextureConverter
{
    /// <summary>
    /// A texture atlas : a texture made from a composition of many textures.
    /// </summary>
    public class TexAtlas : TexImage
    {
        /// <summary>
        /// The atlas inner textures disposition
        /// </summary>
        public TexLayout Layout { get; internal set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="TexAtlas"/> class.
        /// </summary>
        internal TexAtlas()
        {
            Layout = new TexLayout();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="TexAtlas"/> class.
        /// </summary>
        /// <param name="layout">The layout.</param>
        /// <param name="atlas">The atlas.</param>
        public TexAtlas(TexLayout layout, TexImage atlas)
            : base(atlas.Data, atlas.DataSize, atlas.Width, atlas.Height, atlas.Depth, atlas.Format, atlas.MipmapCount, atlas.ArraySize, atlas.Dimension, atlas.FaceCount)
        {
            RowPitch = atlas.RowPitch;
            SlicePitch = atlas.SlicePitch;
            SubImageArray = atlas.SubImageArray;
            Name = atlas.Name;
            DisposingLibrary = atlas.DisposingLibrary;
            CurrentLibrary = atlas.CurrentLibrary;
            LibraryData = atlas.LibraryData;
            Layout = layout;
            Name = "";
        }

        public override Object Clone(bool CopyMemory)
        {
            var atlas = new TexAtlas(Layout, (TexImage)base.Clone(CopyMemory));

            atlas.Layout = new TexLayout();
            foreach (var entry in Layout.TexList)
            {
                atlas.Layout.TexList.Add(entry.Key, entry.Value);
            }

            return atlas;
        }

        /// <summary>
        /// Update the image size and the atlas layout data.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        internal override void Rescale(int width, int height)
        {
            double ratio = (double)width / Width;
            base.Rescale(width, height);

            var texList = new Dictionary<string, TexLayout.Position>();
            TexLayout.Position current;

            foreach (var entry in Layout.TexList)
            {
                current = entry.Value;
                current.Width = (int)(entry.Value.Width * ratio);
                current.Height = (int)(entry.Value.Height * ratio);
                current.UOffset = (int)(entry.Value.UOffset * ratio);
                current.VOffset = (int)(entry.Value.VOffset * ratio);

                texList.Add(entry.Key, current);
            }

            Layout.TexList = texList;
        }


        /// <summary>
        /// Update the atlas layout data.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        internal override void Flip(Orientation orientation)
        {
            var texList = new Dictionary<string, TexLayout.Position>();
            TexLayout.Position current;

            foreach (var entry in Layout.TexList)
            {
                current = entry.Value;
                if (orientation == Orientation.Horizontal) current.UOffset = Width - entry.Value.UOffset - entry.Value.Width;
                else current.VOffset = Height - entry.Value.VOffset - entry.Value.Height;

                texList.Add(entry.Key, current);
            }

            Layout.TexList = texList;
        }


        /// <summary>
        /// Exports the layout data into a file with the same name of the atlas file.
        /// </summary>
        /// <param name="file">The atlas file.</param>
        internal override void Save(string file)
        {
            Layout.Export(Path.ChangeExtension(file, TexLayout.Extension));
        }


        /// <summary>
        /// Describes the positions and size of every inner textures of a texture atlas
        /// </summary>
        public class TexLayout
        {
            /// <summary>
            /// The list of textures position, indexed by their name.
            /// </summary>
            public Dictionary<string, Position> TexList { get; internal set; }


            /// <summary>
            /// The extension of the atlas layout file
            /// </summary>
            public static readonly string Extension = ".ats";


            /// <summary>
            /// Initializes a new instance of the <see cref="TexLayout"/> class.
            /// </summary>
            public TexLayout()
            {
                TexList = new Dictionary<string, Position>();
            }


            /// <summary>
            /// Contains the needed informatio to retrieve an inner texture from the corresponding texture atlas.
            /// </summary>
            public struct Position
            {
                public int UOffset;
                public int VOffset;
                public int Width;
                public int Height;

                public Position(int uOffset, int vVOffset, int width, int height)
                {
                    UOffset = uOffset;
                    VOffset = vVOffset;
                    Width = width;
                    Height = height;
                }

                public override String ToString()
                {
                    return "u:" + UOffset + ", v:" + VOffset + ", w:" + Width + ", h:" + Height;
                }
            }


            /// <summary>
            /// Exports the atlas layout into a specified file.
            /// </summary>
            /// <remarks>
            /// Data are exported into an xml format
            /// </remarks>
            /// <param name="filePath">The file path.</param>
            public void Export(string filePath)
            {
                using (var file = new StreamWriter(@filePath, false))
                {
                    XElement xml = new XElement("texture-list");

                    //file.WriteLine("<texture-list>");
                    foreach (var entry in TexList)
                    {
                        xml.Add(new XElement("texture",
                            new XAttribute("name", entry.Key),
                            new XAttribute("uOffset", entry.Value.UOffset),
                            new XAttribute("vOffset", entry.Value.VOffset),
                            new XAttribute("width", entry.Value.Width),
                            new XAttribute("height", entry.Value.Height)));

                        //file.WriteLine("    <texture name=\"" + entry.Key + "\" uOffset=\"" + entry.Value.UOffset + "\" vOffset=\"" + entry.Value.VOffset + "\" width=\"" + entry.Value.Width + "\" height=\"" + entry.Value.Height + "\" />");
                    }
                    //file.WriteLine("</texture-list>");
                    file.Write(xml);
                }
            }


            /// <summary>
            /// Create an instance of <see cref="TexLayout"/>  from a layout file.
            /// </summary>
            /// <param name="file">The file.</param>
            /// <returns>
            /// A new instance of <see cref="TexLayout"/>.
            /// </returns>
            public static TexLayout Import(string file)
            {
                var texLayout = new TexLayout();

                using (var reader = XmlReader.Create(file))
                {
                    while (reader.ReadToFollowing("texture"))
                    {
                        texLayout.TexList.Add(
                            reader.GetAttribute("name"),
                            new Position(
                                int.Parse(reader.GetAttribute("uOffset")),
                                int.Parse(reader.GetAttribute("vOffset")),
                                int.Parse(reader.GetAttribute("width")),
                                int.Parse(reader.GetAttribute("height"))
                                ));
                    }
                }

                return texLayout;
            }

        }
    }
}
