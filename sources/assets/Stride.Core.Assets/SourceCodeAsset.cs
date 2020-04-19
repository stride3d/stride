// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Assets.TextAccessors;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.Storage;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Class SourceCodeAsset.
    /// </summary>
    [DataContract("SourceCodeAsset")]
    public abstract class SourceCodeAsset : Asset
    {
        [DataMemberIgnore]
        [Display(Browsable = false)]
        public ITextAccessor TextAccessor { get; set; } = new DefaultTextAccessor();
        
        /// <summary>
        /// Used internally by serialization.
        /// </summary>
        [DataMember(DataMemberMode.Assign)]
        [Display(Browsable = false)]
        public ISerializableTextAccessor InternalSerializableTextAccessor
        {
            get { return TextAccessor.GetSerializableVersion(); }
            set { TextAccessor = value.Create(); }
        }

        /// <summary>
        /// Gets the sourcecode text.
        /// </summary>
        /// <value>The sourcecode text.</value>
        [DataMemberIgnore]
        [Display(Browsable = false)]
        public string Text
        {
            get
            {
                return TextAccessor.Get();
            }
            set
            {
                TextAccessor.Set(value);
            }
        }

        /// <summary>
        /// Saves the content to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public virtual void Save(Stream stream)
        {
            TextAccessor.Save(stream).Wait();
        }

        /// <summary>
        /// Generates a unique identifier from location.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Guid.</returns>
        public static AssetId GenerateIdFromLocation(string packageName, string location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            return (AssetId)ObjectId.Combine(ObjectId.FromBytes(Encoding.UTF8.GetBytes(packageName)), ObjectId.FromBytes(Encoding.UTF8.GetBytes(location))).ToGuid();
        }
    }
}
