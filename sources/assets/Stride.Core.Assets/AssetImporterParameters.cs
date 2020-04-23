// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Core;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Parameters used by <see cref="IAssetImporter.Import"/>
    /// </summary>
    public class AssetImporterParameters
    {
        /// <summary>
        /// Gets or sets the logger to use during the import.
        /// </summary>
        public Logger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetImporterParameters"/> class.
        /// </summary>
        public AssetImporterParameters()
        {
            InputParameters = new PropertyCollection();
            SelectedOutputTypes = new Dictionary<Type, bool>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetImporterParameters"/> class.
        /// </summary>
        /// <param name="supportedTypes">The supported types.</param>
        public AssetImporterParameters(params Type[] supportedTypes) : this((IEnumerable<Type>)supportedTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetImporterParameters"/> class.
        /// </summary>
        /// <param name="supportedTypes">The supported types.</param>
        /// <exception cref="System.ArgumentNullException">supportedTypes</exception>
        /// <exception cref="System.ArgumentException">Invalid type [{0}]. Type must be assignable to Asset.ToFormat(type);supportedTypes</exception>
        public AssetImporterParameters(IEnumerable<Type> supportedTypes) : this()
        {
            if (supportedTypes == null) throw new ArgumentNullException("supportedTypes");
            foreach (var type in supportedTypes)
            {
                if (!typeof(Asset).IsAssignableFrom(type))
                {
                    throw new ArgumentException("Invalid type [{0}]. Type must be assignable to Asset".ToFormat(type), "supportedTypes");
                }
                SelectedOutputTypes[type] = true;
            }
        }

        /// <summary>
        /// Gets the import input parameters.
        /// </summary>
        /// <value>The import input parameters.</value>
        public PropertyCollection InputParameters { get; private set; }

        /// <summary>
        /// Gets the selected output types.
        /// </summary>
        /// <value>The selected output types.</value>
        public Dictionary<Type, bool> SelectedOutputTypes { get; private set; }

        /// <summary>
        /// Determines whether the specified type is type selected for output by this importer.
        /// </summary>
        /// <typeparam name="T">A Type asset </typeparam>
        /// <returns><c>true</c> if the specified type is type selected for output by this importer; otherwise, <c>false</c>.</returns>
        public bool IsTypeSelectedForOutput<T>() where T : Asset
        {
            return IsTypeSelectedForOutput(typeof(T));
        }

        /// <summary>
        /// Determines whether the specified type is type selected for output by this importer.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is type selected for output by this importer; otherwise, <c>false</c>.</returns>
        public bool IsTypeSelectedForOutput(Type type)
        {
            bool isSelected;
            if (SelectedOutputTypes.TryGetValue(type, out isSelected))
            {
                return isSelected;
            }
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has valid selected output types.
        /// </summary>
        /// <value><c>true</c> if this instance has selected output types; otherwise, <c>false</c>.</value>
        public bool HasSelectedOutputTypes
        {
            get
            {
                return SelectedOutputTypes.Count > 0 && SelectedOutputTypes.Any(selectedOutputType => selectedOutputType.Value);
            }
        }
    }
}
