// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Xml.Linq;
namespace Stride.ProjectGenerator
{
    public interface IProjectProcessor
    {
        void Process(ProjectProcessorContext context);
    }
}
