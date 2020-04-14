// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;

namespace Stride.Core.Yaml.Serialization.Serializers
{
    internal static class ExceptionUtils
    {
        /// <summary>
        /// Unwraps some exception such as <see cref="TargetInvocationException"/>.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static Exception Unwrap(this Exception exception)
        {
            var targetInvocationException = exception as TargetInvocationException;
            if (targetInvocationException != null)
                return targetInvocationException.InnerException;

            return exception;
        }
    }
}
