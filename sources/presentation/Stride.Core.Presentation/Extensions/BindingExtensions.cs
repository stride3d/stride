// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Windows.Data;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Extensions
{
    public static class BindingExtensions
    {
        private static readonly MethodInfo CloneMethodInfo = typeof(BindingBase).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// This methods is a wrapper to the internal method Clone of the <see cref="BindingBase"/>. Using this method should be considered unsafe.
        /// </summary>
        /// <param name="binding">The Binding to clone.</param>
        /// <returns>A clone of the given <see cref="Binding"/></returns>
        public static Binding CloneBinding([NotNull] this Binding binding)
        {
            return (Binding)binding.CloneBinding(binding.Mode);
        }
        /// <summary>
        /// This methods is a wrapper to the internal method Clone of the <see cref="BindingBase"/>. Using this method should be considered unsafe.
        /// </summary>
        /// <param name="bindingBase">The BindingBase to clone.</param>
        /// <param name="mode">The BindingMode value to set for the clone.</param>
        /// <returns>A clone of the given <see cref="BindingBase"/></returns>
        public static BindingBase CloneBinding([NotNull] this BindingBase bindingBase, BindingMode mode)
        {
            return (BindingBase)CloneMethodInfo.Invoke(bindingBase, new object[] { mode });
        }
    }
}
