// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Xenko.ConfigEditor
{
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class Options
    {
        private string xenkoPath;
        [XmlElement]
        public string XenkoPath
        {
            get { return xenkoPath; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Invalid 'XenkoPath' property value");
                xenkoPath = value;
            }
        }

        [XmlElement]
        public string XenkoConfigFilename { get; set; }

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
