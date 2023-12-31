using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.DebugRender.Components
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Editor)]
    [ComponentCategory("Bepu - Debug")]
    public class DebugRenderComponent : EntityComponent
    {

    }
}
