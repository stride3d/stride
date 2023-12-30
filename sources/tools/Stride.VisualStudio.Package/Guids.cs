// Guids.cs
// MUST match guids.h
using System;

namespace Stride.VisualStudio
{
    internal static class GuidList
    {
        public const string guidStride_VisualStudio_PackagePkgString = "248ff1ce-dacd-4404-947a-85e999d3c3ea";
        public const string guidStride_VisualStudio_PackageCmdSetString = "9428db93-bfea-4115-8d4a-40b047166e61";

        public const string guidStride_VisualStudio_ShaderKeyFileGenerator = "b50e6ece-b11f-477b-a8e1-1e60e0531a53";
        public const string guidStride_VisualStudio_EffectCodeFileGenerator = "e6259cfb-c775-426e-b499-f57d0a3ba2c1";

        public const string guidStride_VisualStudio_DataCodeGenerator = "22555301-d58a-4d71-9dab-b2552cc3de0e";

        public static readonly Guid guidStride_VisualStudio_PackageCmdSet = new Guid(guidStride_VisualStudio_PackageCmdSetString);

        public const string vsContextGuidVCSProject = "{fae04ec1-301f-11d3-bf4b-00c04f79efbc}";
        public const string vsContextGuidVCSNewProject = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
    };
}
