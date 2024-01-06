using Stride.BepuPhysics.DebugRender.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.DebugRender.Components
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(DebugRenderProcessor), ExecutionMode = ExecutionMode.Editor | ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Debug")]
    public class DebugRenderComponent : StartupScript
    {
        //Temp

        internal Action<bool>? SetFunc;
        private bool _alwaysRender = true;

        [DataMember]
        public bool AlwaysRender
        {
            get => _alwaysRender; 
            set
            {
                _alwaysRender = value;
                SetFunc?.Invoke(_alwaysRender);
            }
        }
    }
}
