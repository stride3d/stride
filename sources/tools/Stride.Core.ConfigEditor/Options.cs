// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Stride.ConfigEditor
{
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class Options
    {
        private string stridePath;
        [XmlElement]
        public string StridePath
        {
            get { return stridePath; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Invalid 'StridePath' property value");
                stridePath = value;
            }
        }

        [XmlElement]
        public string StrideConfigFilename { get; set; }

        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(Options));

        public static Options Load()
        {
            try
            {
                var filename = Path.ChangeExtension(Assembly.GetEntryAssembly().Location, ".config");
                return (Options)serializer.Deserialize(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            }
            catch
            {
                return null;
            }
        }

        public void Save()
        {
            var filename = Path.ChangeExtension(Assembly.GetEntryAssembly().Location, ".config");
            serializer.Serialize(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), this);
        }
    }
}
