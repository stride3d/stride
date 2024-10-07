using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;


public partial class Mixer
{
    public struct EntryPoint
    {
        public Mixer mixer;
        public ExecutionModel ExecutionModel { get; }
        public string Name { get; }

        Instruction function;
        public EntryPoint(Mixer mixer, ExecutionModel executionModel, string name)
        {
            this.mixer = mixer;
            ExecutionModel = executionModel;
            Name = name;
        }

        

        public FunctionBuilder FunctionStart()
        {
            return new(mixer,this);
        }

        public Mixer FinishEntryPoint()
        {
            mixer.Buffer.AddOpEntryPoint(ExecutionModel, function, Name, Span<IdRef>.Empty);
            mixer.Buffer.AddOpExecutionMode(
                function,
                ExecutionMode.OriginLowerLeft
            );
            mixer.Buffer.AddOpCapability(Capability.Shader);
            mixer.Buffer.AddOpExtInstImport("GLSL.std.450");
            mixer.Buffer.AddOpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);
            return mixer;
        }
    }
}