using Stride.BepuPhysics.DebugRender.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.DebugRender.Components
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(DebugRenderProcessor), ExecutionMode = ExecutionMode.Editor | ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Debug")]
    public class DebugRenderComponent : EntityComponent
    {

    }
}
