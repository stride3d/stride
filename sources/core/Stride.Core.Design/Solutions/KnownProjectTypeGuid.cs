#region License

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.
//
// SLNTools
// Copyright (c) 2009
// by Christian Warren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#endregion

namespace Stride.Core.Solutions;

public static class KnownProjectTypeGuid
{
    public static readonly Guid VisualBasic = new("F184B08F-C81C-45F6-A57F-5ABD9991F28F");
    // C# project type guid that forces the SDK project system in Visual Studio. Stride emits this for its
    // .csproj entries because its projects import the SDK via MSBuild <Import> instead of the
    // <Project Sdk="..."> attribute that VS otherwise keys on to recognize an SDK-style project.
    public static readonly Guid CSharp = new("9A19103F-16F7-4668-BE54-9A1E7A4F7556");
    // The original C# project type guid. Visual Studio and `dotnet sln add` still emit it for every C# project
    // (SDK-style or not — SDK behavior comes from the csproj's Sdk attribute); accepted when reading solutions.
    public static readonly Guid CSharpLegacy = new("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
    public static readonly Guid JSharp = new("E6FDF86B-F3D1-11D4-8576-0002A516ECE8");
    public static readonly Guid FSharp = new("F2A71F9B-5D33-465A-A702-920D77279786");
    public static readonly Guid SolutionFolder = new("2150E333-8FDC-42A3-9474-1A3956D46DE8");
    public static readonly Guid VisualC = new("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942");
    public static readonly Guid Setup = new("54435603-DBB4-11D2-8724-00A0C9A8B90C");
    public static readonly Guid WebProject = new("E24C65DC-7377-472B-9ABA-BC803B73C61A");
}
