using System;
using Silk.NET.OpenXR;

namespace Stride.VirtualReality
{
    internal sealed class OpenXRException : Exception
    {
        public OpenXRException(Result result, string methodName)
            : base($"{methodName} returned {result}")
        {
            Result = result;
            MethodName = methodName;
        }

        public Result Result { get; }

        public string MethodName { get; }
    }
}
