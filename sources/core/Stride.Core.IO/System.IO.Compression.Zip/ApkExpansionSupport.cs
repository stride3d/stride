// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApkExpansionSupport.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The apk expansion support.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace System.IO.Compression.Zip
{
    using System.Collections.Generic;

    using Android.Content;
    using Android.OS;

    /// <summary>
    /// The apk expansion support.
    /// </summary>
    public static class ApkExpansionSupport
    {
        #region Constants

        /// <summary>
        /// The shared path to all app expansion files
        /// </summary>
        private const string ExpansionFilesPath = "/Android/obb/";

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get apk expansion files.
        /// </summary>
        /// <param name="ctx">
        /// The ctx.
        /// </param>
        /// <param name="mainVersion">
        /// The main version.
        /// </param>
        /// <param name="patchVersion">
        /// The patch version.
        /// </param>
        /// <returns>
        /// A list of obb files for this app.
        /// </returns>
        public static IEnumerable<string> GetApkExpansionFiles(Context ctx, int mainVersion, int patchVersion)
        {
            var ret = new List<string>();

            if (Environment.ExternalStorageState.Equals(Environment.MediaMounted))
            {
                string packageName = ctx.PackageName;

                // Build the full path to the app's expansion files
                string expPath = Environment.ExternalStorageDirectory + ExpansionFilesPath + packageName;

                // Check that expansion file path exists
                if (Directory.Exists(expPath))
                {
                    string main = Path.Combine(expPath, string.Format("main.{0}.{1}.obb", mainVersion, packageName));
                    string patch = Path.Combine(expPath, string.Format("patch.{0}.{1}.obb", mainVersion, packageName));

                    if (mainVersion > 0 && File.Exists(main))
                    {
                        ret.Add(main);
                    }

                    if (patchVersion > 0 && File.Exists(patch))
                    {
                        ret.Add(patch);
                    }
                }
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Gets a <see cref="ExpansionZipFile"/> that contains a list of all 
        /// the files in the combined main and patch obb packages.
        /// </summary>
        /// <param name="ctx">
        /// The context.
        /// </param>
        /// <param name="mainVersion">
        /// The main obb version.
        /// </param>
        /// <param name="patchVersion">
        /// The patch obb version.
        /// </param>
        /// <returns>
        /// The apk expansion zip file.
        /// </returns>
        public static ExpansionZipFile GetApkExpansionZipFile(Context ctx, int mainVersion, int patchVersion)
        {
            return new ExpansionZipFile(GetApkExpansionFiles(ctx, mainVersion, patchVersion));
        }

        #endregion
    }
}
