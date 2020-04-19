// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExpansionZipFile.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The expansion zip file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace System.IO.Compression.Zip
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The expansion zip file.
    /// </summary>
    public class ExpansionZipFile
    {
        #region Fields

        /// <summary>
        /// The files.
        /// </summary>
        private readonly Dictionary<string, ZipFileEntry> files;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> class.
        /// </summary>
        /// <param name="entries">
        /// The entries.
        /// </param>
        public ExpansionZipFile(IEnumerable<ZipFileEntry> entries)
        {
            this.files = entries.ToDictionary(x => x.FilenameInZip, x => x);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> class.
        /// </summary>
        public ExpansionZipFile()
        {
            this.files = new Dictionary<string, ZipFileEntry>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> class. 
        /// class from a collection of zip file paths.
        /// </summary>
        /// <param name="zipPaths">
        /// Collection of zip file paths
        /// </param>
        public ExpansionZipFile(IEnumerable<string> zipPaths)
            : this()
        {
            foreach (string expansionFile in zipPaths)
            {
                this.MergeZipFile(expansionFile);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Add all the entries from an existing <see cref="ExpansionZipFile"/>
        /// </summary>
        /// <param name="merge">
        /// The ExpansionZipFile to use.
        /// </param>
        public void AddZipFileEntries(IEnumerable<ZipFileEntry> merge)
        {
            foreach (var entry in merge.ToDictionary(x => x.FilenameInZip, x => x))
            {
                if (this.files.ContainsKey(entry.Key))
                {
                    this.files[entry.Key] = entry.Value;
                }
                else
                {
                    this.files.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// The get all entries.
        /// </summary>
        /// <returns>
        /// A list of all the entries for this app.
        /// </returns>
        public ZipFileEntry[] GetAllEntries()
        {
            return this.files.Values.ToArray();
        }

        /// <summary>
        /// The get entry.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The entry at a specified relative path
        /// </returns>
        public ZipFileEntry GetEntry(string path)
        {
            return this.files.ContainsKey(path) ? this.files[path] : null;
        }

        /// <summary>
        /// Add all the entries from an existing <see cref="ExpansionZipFile"/>
        /// </summary>
        /// <param name="merge">
        /// The ExpansionZipFile to use.
        /// </param>
        public void MergeZipFile(ExpansionZipFile merge)
        {
            foreach (var entry in merge.files)
            {
                if (this.files.ContainsKey(entry.Key))
                {
                    this.files[entry.Key] = entry.Value;
                }
                else
                {
                    this.files.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Add all the entries from a zip file on the system.
        /// </summary>
        /// <param name="path">
        /// Path to the zip file
        /// </param>
        public void MergeZipFile(string path)
        {
            ZipFileEntry[] merge;
            using (var zip = new ZipFile(path))
            {
                merge = zip.GetAllEntries();
            }

            this.AddZipFileEntries(merge);
        }

        #endregion
    }
}
