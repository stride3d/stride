// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml;
using Stride.Core.Annotations;

namespace Stride.Core.Translation.Presentation.MarkupExtensions
{
    internal static class MarkupExtensionHelper
    {
        /// <summary>
        /// Retrieves the local assembly corresponding to the context where the <see cref="MarkupExtension"/> is used.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>The local assembly in the context of the <see cref="MarkupExtension"/>.</returns>
        /// <remarks>Should be called from the <see cref="MarkupExtension.ProvideValue"/> method.</remarks>
        [NotNull]
        public static Assembly RetrieveLocalAssembly([NotNull] IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            Assembly assembly = null;
            try
            {
                // get the assembly name from the IUriContext
                var uriContext = (IUriContext)serviceProvider.GetService(typeof(IUriContext));
                var localPath = uriContext.BaseUri?.LocalPath;
                var assemblyName = localPath?.Substring(1, localPath.IndexOf(";", StringComparison.InvariantCultureIgnoreCase) - 1);
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    assembly = Assembly.Load(assemblyName);
                }
            }
            catch (SystemException)
            {
            }
            var rootProvider = (IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider));
            if (assembly == null)
            {
                try
                {
                    // Hack: get the real service provider (not the wrapper)
                    // get the assembly from reflection
                    var xamlContextField = rootProvider.GetType().GetMember("_xamlContext", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault() as FieldInfo;
                    var xamlContext = xamlContextField?.GetValue(rootProvider);
                    var localAssemblyProperty = xamlContext?.GetType().GetProperty("LocalAssembly", BindingFlags.Public | BindingFlags.Instance);
                    assembly = localAssemblyProperty?.GetValue(xamlContext) as Assembly;
                }
                catch (SystemException)
                {
                }
            }
            if (assembly == null)
            {
                // get the assembly name from the IRootObjectProvider (will not provide the expected value when used in xaml theme or resource files)
                assembly = rootProvider.RootObject.GetType().Assembly;
            }
            return assembly;
        }
    }
}
