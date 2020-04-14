// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.SpriteStudio.Runtime;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Stride.SpriteStudio.Offline
{
    internal class SpriteStudioXmlImport
    {
        public static SpriteStudioBlending ParseBlending(string str)
        {
            switch (str)
            {
                case "mix":
                    return SpriteStudioBlending.Mix;

                case "add":
                    return SpriteStudioBlending.Addition;

                case "mul":
                    return SpriteStudioBlending.Multiplication;

                case "sub":
                    return SpriteStudioBlending.Subtraction;
            }

            return SpriteStudioBlending.Mix;
        }

        private static void FillNodeData(XNamespace nameSpace, XContainer part, List<SpriteStudioCell> cells, out NodeAnimationData nodeData)
        {
            nodeData = new NodeAnimationData();

            var attribs = part.Descendants(nameSpace + "attribute");
            foreach (var attrib in attribs)
            {
                var tag = attrib.Attributes("tag").First().Value;
                var keys = attrib.Descendants(nameSpace + "key");

                switch (tag)
                {
                    case "CELL":
                        {
                            var keyValues = new List<Dictionary<string, string>>();
                            foreach (var key in keys)
                            {
                                var values = new Dictionary<string, string>();
                                var cellName = key.Descendants(nameSpace + "value").First().Descendants(nameSpace + "name").First().Value;
                                var index = 0;
                                var realIndex = -1;
                                foreach (var cell in cells)
                                {
                                    if (cell.Name == cellName)
                                    {
                                        realIndex = index;
                                    }
                                    index++;
                                }
                                values.Add("time", key.Attribute("time").Value);
                                values.Add("curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear");
                                values.Add("value", realIndex.ToString(CultureInfo.InvariantCulture));

                                keyValues.Add(values);
                            }
                            nodeData.Data.Add(tag, keyValues);
                        }
                        break;

                    case "VCOL":
                        {
                            var xElements = keys as XElement[] ?? keys.ToArray();
                            var blendValues = new List<Dictionary<string, string>>();
                            foreach (var key in xElements)
                            {
                                var values = new Dictionary<string, string>();
                                var blendType = key.Descendants(nameSpace + "value").First().Descendants(nameSpace + "blendType").First().Value;

                                values.Add("time", key.Attribute("time").Value);
                                values.Add("curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear");
                                values.Add("value", ((int)ParseBlending(blendType)).ToString(CultureInfo.InvariantCulture));

                                blendValues.Add(values);
                            }
                            nodeData.Data.Add("COLB", blendValues);

                            var colorValues = new List<Dictionary<string, string>>();
                            foreach (var key in xElements)
                            {
                                var values = new Dictionary<string, string>();
                                var colorInt = key.Descendants(nameSpace + "value").First().Descendants(nameSpace + "rgba").First().Value;
                                var color = int.Parse(colorInt, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                                values.Add("time", key.Attribute("time").Value);
                                values.Add("curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear");
                                values.Add("value", color.ToString(CultureInfo.InvariantCulture));

                                colorValues.Add(values);
                            }
                            nodeData.Data.Add("COLV", colorValues);

                            var factorValues = new List<Dictionary<string, string>>();
                            foreach (var key in xElements)
                            {
                                var values = new Dictionary<string, string>();
                                var rateValue = key.Descendants(nameSpace + "value").First().Descendants(nameSpace + "rate").First().Value;

                                values.Add("time", key.Attribute("time").Value);
                                values.Add("curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear");
                                values.Add("value", rateValue);

                                factorValues.Add(values);
                            }
                            nodeData.Data.Add("COLF", factorValues);
                        }
                        break;

                    default:
                        {
                            var values = keys.Where(key => key.Descendants(nameSpace + "value").FirstOrDefault() != null).Select(key => new Dictionary<string, string>
                            {
                                { "time", key.Attribute("time").Value },
                                { "curve", key.Attribute("ipType") != null ? key.Attribute("ipType").Value : "linear" },
                                { "value", key.Descendants(nameSpace + "value").First().Value }
                            }).ToList();
                            nodeData.Data.Add(tag, values);
                        }
                        break;
                }
            }
        }

        public static bool ParseAnimations(UFile file, List<SpriteStudioAnim> animations)
        {
            var textures = new List<UFile>();
            var cells = new List<SpriteStudioCell>();
            if (!ParseCellMaps(file, textures, cells)) return false;

            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            var anims = xmlDoc.Descendants(nameSpace + "anime");
            foreach (var animXml in anims)
            {
                var animName = animXml.Descendants(nameSpace + "name").First().Value;

                int fps, frameCount;
                if (!int.TryParse(animXml.Descendants(nameSpace + "fps").First().Value, out fps)) continue;
                if (!int.TryParse(animXml.Descendants(nameSpace + "frameCount").First().Value, out frameCount)) continue;

                var anim = new SpriteStudioAnim
                {
                    Name = animName,
                    Fps = fps,
                    FrameCount = frameCount
                };

                var animParts = animXml.Descendants(nameSpace + "partAnime");
                foreach (var animPart in animParts)
                {
                    NodeAnimationData data;
                    FillNodeData(nameSpace, animPart, cells, out data);
                    anim.NodesData.Add(animPart.Descendants(nameSpace + "partName").First().Value, data);
                }

                animations.Add(anim);
            }

            return true;
        }

        public static bool ParseModel(UFile file, List<SpriteStudioNode> nodes, out string modelName)
        {
            modelName = string.Empty;

            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            modelName = xmlDoc.Descendants(nameSpace + "name").First().Value;

            var model = xmlDoc.Descendants(nameSpace + "Model").First();
            var modelNodes = model.Descendants(nameSpace + "value");
            foreach (var xmlNode in modelNodes)
            {
                var nodeName = xmlNode.Descendants(nameSpace + "name").First().Value;
                var isNull = xmlNode.Descendants(nameSpace + "type").First().Value == "null";
                int nodeId, parentId;
                if (!int.TryParse(xmlNode.Descendants(nameSpace + "arrayIndex").First().Value, out nodeId)) continue;
                if (!int.TryParse(xmlNode.Descendants(nameSpace + "parentIndex").First().Value, out parentId)) continue;

                var blendingName = xmlNode.Descendants(nameSpace + "alphaBlendType").First().Value;

                var shouldInherit = parentId != -1 && xmlNode.Descendants(nameSpace + "inheritType").First().Value == "parent";

                var node = new SpriteStudioNode
                {
                    Name = nodeName,
                    Id = nodeId,
                    ParentId = parentId,
                    IsNull = isNull,
                    NoInheritance = !shouldInherit
                };

                var inheritances = xmlNode.Descendants(nameSpace + "ineheritRates");
                var xElements = inheritances as XElement[] ?? inheritances.ToArray();
                var alphaInh = xElements.Descendants(nameSpace + "ALPH").FirstOrDefault();
                if (alphaInh != null)
                {
                    node.AlphaInheritance = alphaInh.Value == "1";
                }
                var flphInh = xElements.Descendants(nameSpace + "FLPH").FirstOrDefault();
                if (flphInh != null)
                {
                    node.FlphInheritance = flphInh.Value == "1";
                }
                var flpvInh = xElements.Descendants(nameSpace + "FLPV").FirstOrDefault();
                if (flpvInh != null)
                {
                    node.FlpvInheritance = flpvInh.Value == "1";
                }
                var hideInh = xElements.Descendants(nameSpace + "HIDE").FirstOrDefault();
                if (hideInh != null)
                {
                    node.HideInheritance = hideInh.Value == "1";
                }

                node.AlphaBlending = ParseBlending(blendingName);

                nodes.Add(node);
            }

            //pre process inheritances
            foreach (var node in nodes)
            {
                if (node.NoInheritance) continue;

                //go find which parent node controls us
                var parentId = node.ParentId;
                while (parentId != -1)
                {
                    var parent = nodes[parentId];

                    if (parent.NoInheritance)
                    {
                        node.AlphaInheritance = parent.AlphaInheritance;
                        node.FlphInheritance = parent.FlphInheritance;
                        node.FlpvInheritance = parent.FlpvInheritance;
                        node.HideInheritance = parent.HideInheritance;
                        break;
                    }

                    parentId = parent.ParentId;
                }
            }

            return true;
        }

        public static bool ParseCellMaps(UFile file, List<UFile> textures, List<SpriteStudioCell> cells)
        {
            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            var cellMaps = xmlDoc.Descendants(nameSpace + "cellmapNames").Descendants(nameSpace + "value");

            foreach (var cellMap in cellMaps)
            {
                var mapFile = UPath.Combine(file.GetFullDirectory(), new UFile(cellMap.Value));
                var cellDoc = XDocument.Load(mapFile);
                if (cellDoc.Root == null) return false;

                var cnameSpace = cellDoc.Root.Name.Namespace;

                var cellNodes = cellDoc.Descendants(nameSpace + "cell");
                foreach (var cellNode in cellNodes)
                {
                    var cell = new SpriteStudioCell
                    {
                        Name = cellNode.Descendants(cnameSpace + "name").First().Value,
                        TextureIndex = textures.Count
                    };

                    var posData = cellNode.Descendants(nameSpace + "pos").First().Value;
                    var posValues = Regex.Split(posData, "\\s+");
                    var sizeData = cellNode.Descendants(nameSpace + "size").First().Value;
                    var sizeValues = Regex.Split(sizeData, "\\s+");
                    cell.Rectangle = new RectangleF(
                        float.Parse(posValues[0], CultureInfo.InvariantCulture),
                        float.Parse(posValues[1], CultureInfo.InvariantCulture),
                        float.Parse(sizeValues[0], CultureInfo.InvariantCulture),
                        float.Parse(sizeValues[1], CultureInfo.InvariantCulture));

                    var pivotData = cellNode.Descendants(nameSpace + "pivot").First().Value;
                    var pivotValues = Regex.Split(pivotData, "\\s+");
                    cell.Pivot = new Vector2((float.Parse(pivotValues[0], CultureInfo.InvariantCulture) + 0.5f) * cell.Rectangle.Width, (-float.Parse(pivotValues[1], CultureInfo.InvariantCulture) + 0.5f) * cell.Rectangle.Height);

                    cells.Add(cell);
                }

                var textPath = cellDoc.Descendants(nameSpace + "imagePath").First().Value;

                textures.Add(UPath.Combine(file.GetFullDirectory(), new UFile(textPath)));
            }

            return true;
        }

        public static bool SanityCheck(UFile file)
        {
            var xmlDoc = XDocument.Load(file);
            if (xmlDoc.Root == null) return false;

            var nameSpace = xmlDoc.Root.Name.Namespace;

            var cellMaps = xmlDoc.Descendants(nameSpace + "cellmapNames").Descendants(nameSpace + "value").ToList();
            return cellMaps.Select(cellMap => UPath.Combine(file.GetFullDirectory(), new UFile(cellMap.Value))).All(fileName => File.Exists(fileName.ToWindowsPath()));
        }
    }
}
