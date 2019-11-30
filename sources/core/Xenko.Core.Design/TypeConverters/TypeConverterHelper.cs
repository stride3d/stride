// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core.Annotations;

namespace Xenko.Core.TypeConverters
{
    public static class TypeConverterHelper
    {
        /// <summary>
        /// Returns whether an instance of <paramref name="sourceType"/> can be converted to an instance of <paramref name="destinationType"/>.
        /// </summary>
        /// <param name="sourceType">A <see cref="Type"/> that represents the type you want to convert from.</param>
        /// <param name="destinationType">A <see cref="Type"/> that represents the type you want to convert to.</param>
        /// <returns><c>true</c> if such a conversion exists; otherwise, <c>false</c>.</returns>
        public static bool CanConvert([NotNull] Type sourceType, [NotNull] Type destinationType)
        {
            if (sourceType == null) throw new ArgumentNullException(nameof(sourceType));
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            var context = new DestinationTypeDescriptorContext(destinationType);
            // already same type or inherited (also works with interface), or
            // implements IConvertible, or
            // can convert from source type to target type
            return destinationType.IsAssignableFrom(sourceType) ||
                   (typeof(IConvertible).IsAssignableFrom(sourceType) && Type.GetTypeCode(destinationType) != TypeCode.Object) ||
                   TypeDescriptor.GetConverter(sourceType).CanConvertTo(destinationType) || TypeDescriptor.GetConverter(destinationType).CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Tries to convert the <paramref name="source"/> to the <paramref name="destinationType"/>.
        /// </summary>
        /// <param name="source">The object to convert</param>
        /// <param name="destinationType">The type to convert to</param>
        /// <param name="target">The converted object</param>
        /// <returns><c>true</c> if the <paramref name="source"/> could be converted to the <paramref name="destinationType"/>; otherwise, <c>false</c>.</returns>
        public static bool TryConvert(object source, [NotNull] Type destinationType, out object target)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (source != null)
            {
                try
                {
                    // Already same type or inherited (also works with interface)
                    if (destinationType.IsInstanceOfType(source))
                    {
                        target = source;
                        return true;
                    }

                    if (source is IConvertible)
                    {
                        var typeCode = Type.GetTypeCode(destinationType);
                        if (typeCode != TypeCode.Object)
                        {
                            target = Convert.ChangeType(source, destinationType);
                            return true;
                        }
                    }

                    var sourceType = source.GetType();
                    // Try to convert using the source type converter
                    var converter = TypeDescriptor.GetConverter(sourceType);
                    if (converter.CanConvertTo(destinationType))
                    {
                        target = converter.ConvertTo(source, destinationType);
                        return true;
                    }
                    // Try to convert using the target type converter
                    var context = new DestinationTypeDescriptorContext(destinationType);
                    converter = TypeDescriptor.GetConverter(destinationType);
                    if (converter.CanConvertFrom(context, sourceType))
                    {
                        target = converter.ConvertFrom(context, System.Globalization.CultureInfo.CurrentCulture, source);
                        return true;
                    }
                }
                catch (InvalidCastException) { }
                catch (InvalidOperationException) { }
                catch (FormatException) { }
                catch (NotSupportedException) { }
                catch (OverflowException) { }
                catch (Exception ex) when (ex.InnerException is InvalidCastException) { }
                catch (Exception ex) when (ex.InnerException is InvalidOperationException) { }
                catch (Exception ex) when (ex.InnerException is FormatException) { }
                catch (Exception ex) when (ex.InnerException is NotSupportedException) { }
                catch (Exception ex) when (ex.InnerException is OverflowException) { }
            }

            // Incompatible type and no conversion available
            target = null;
            return false;
        }

        public static Type GetDestinationType(ITypeDescriptorContext context)
        {
            if (context is DestinationTypeDescriptorContext c) return c.DestinationType;

            return null;
        }

        private class DestinationTypeDescriptorContext : ITypeDescriptorContext
        {
            public DestinationTypeDescriptorContext(Type destinationType)
            {
                DestinationType = destinationType;
            }

            public Type DestinationType { get; }

            public IContainer Container => null;

            public object Instance => null;

            public PropertyDescriptor PropertyDescriptor => null;

            public object GetService(Type serviceType)
            {
                return null;
            }

            public void OnComponentChanged()
            {
                
            }

            public bool OnComponentChanging()
            {
                return true;
            }
        }
    }
}
