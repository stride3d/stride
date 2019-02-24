using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Xenko.Core.Assets;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Represents a Url to an asset.
    /// </summary>
    [DataContract]
    public class UrlReference
    {
        /// <summary>
        /// Create a new <see cref="UrlReference"/> instance.
        /// </summary>
        /// <param name="url"></param>
        public UrlReference(AssetId id, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException($"{nameof(url)} cannot be null or empty.", nameof(url));
            }

            Id = id;
            Url = url;
        }

        public UrlReference()
        {

        }

        [DataMember]
        [Display(Browsable = false)]
        public string Url { get; set; }

        [DataMember]
        [Display(Browsable = false)]
        public AssetId Id { get; internal set; }

        /// <inheritdoc />
        public override string ToString() => Url;


        
    }

    /// <summary>
    /// Represents a Url to an asset of type <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T">The type off asset.</typeparam>
    [DataContract]
    public class UrlReference<T> : UrlReference
        where T : class
    {
        /// <summary>
        /// Create a new <see cref="UrlReference{T}"/> instance.
        /// </summary>
        /// <param name="url"></param>
        public UrlReference(AssetId id, string url) : base(id, url)
        {
        }

        public UrlReference()
        {

        }
    }

    
}
