// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// A compiled version of <see cref="MaterialDescriptor"/>.
    /// </summary>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Material>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<Material>))]
    [DataContract]
    public class Material
    {
        private static readonly Logger Log = GlobalLogger.GetLogger(nameof(Material));

        /// <summary>
        /// Initializes a new instance of the <see cref="Material"/> class.
        /// </summary>
        public Material()
        {
            Passes = new MaterialPassCollection(this);
        }

        /// <summary>
        /// The passes contained in this material (usually one).
        /// </summary>
        public MaterialPassCollection Passes { get; }

        /// <summary>
        /// Gets or sets the descriptor (this field is null at runtime).
        /// </summary>
        /// <value>The descriptor.</value>
        [DataMemberIgnore]
        public MaterialDescriptor Descriptor { get; set; }

        /// <summary>
        /// Creates a new material from the specified descriptor.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="descriptor">The material descriptor.</param>
        /// <param name="content">
        /// The content manager that loads the assets the material's features reference, such as
        /// <c>Game.Content</c>. Without one, those references stay empty objects and the material reports
        /// each one it could not load.
        /// </param>
        /// <returns>An instance of a <see cref="Material"/>.</returns>
        /// <exception cref="System.ArgumentNullException">descriptor</exception>
        /// <exception cref="System.InvalidOperationException">If an error occurs with the material description</exception>
        public static Material New(GraphicsDevice device, MaterialDescriptor descriptor, ContentManager content = null)
        {
            if (descriptor == null) throw new ArgumentNullException("descriptor");

            // The descriptor is not assigned to the material because
            // 1) we don't know whether it will mutate and be used to generate another material
            // 2) we don't wanna hold on to memory we actually don't need
            var context = new MaterialGeneratorContext(new Material(), device)
            {
                GraphicsProfile = device.Features.RequestedProfile,
            };
            var result = MaterialGenerator.Generate(descriptor, context, string.Format("{0}:RuntimeMaterial", descriptor.MaterialId));

            if (result.HasErrors)
            {
                throw new InvalidOperationException(string.Format("Error when creating the material [{0}]", result.ToText()));
            }

            var material = result.Material;

            ResolveAttachedReferences(material, content);

            return material;
        }

        /// <summary>
        /// Loads the assets that the generated material only holds references to.
        /// </summary>
        /// <param name="material">The generated material.</param>
        /// <param name="content">The content manager to load through, or <c>null</c> to only report.</param>
        /// <remarks>
        /// A material feature attaches a reference with <see cref="AttachedReferenceManager.CreateProxyObject{T}(AssetId, string)"/>,
        /// which makes an empty object and marks it as a proxy. A proxy becomes the real asset in one
        /// place only, <c>ReferenceSerializer</c>, and only during a content load. The generator does not
        /// run there, so the material keeps empty objects until they are loaded here.
        /// </remarks>
        private static void ResolveAttachedReferences(Material material, ContentManager content)
        {
            foreach (var pass in material.Passes)
            {
                var parameters = pass.Parameters;
                var objectValues = parameters.ObjectValues;

                if (objectValues is null)
                    continue;

                foreach (var keyInfo in parameters.ParameterKeyInfos)
                {
                    if (!keyInfo.IsResourceParameter || keyInfo.BindingSlot >= objectValues.Length)
                        continue;

                    var reference = AttachedReferenceManager.GetAttachedReference(objectValues[keyInfo.BindingSlot]);
                    if (reference is not { IsProxy: true })
                        continue;

                    if (content is null)
                    {
                        Log.Warning($"Material parameter '{keyInfo.Key}' keeps an empty object, because no " +
                                    $"content manager was given to load '{reference.Url}'. Pass one to " +
                                    $"{nameof(Material)}.{nameof(New)}, such as Game.Content.");
                        continue;
                    }

                    try
                    {
                        objectValues[keyInfo.BindingSlot] = content.Load(keyInfo.Key.PropertyType, reference.Url);
                    }
                    catch (ContentManagerException exception)
                    {
                        // An asset reaches a build only when something in the content references it.
                        // Nothing references what a material feature attaches here, because that happens
                        // at run time, so a game that builds all of its materials in code can lack the
                        // asset. Keep the empty object rather than stop the game, and say what is wrong.
                        Log.Warning($"Material parameter '{keyInfo.Key}' keeps an empty object, because the " +
                                    $"asset it references is not in the build: '{reference.Url}'. {exception.Message}");
                    }
                }
            }
        }
    }
}
