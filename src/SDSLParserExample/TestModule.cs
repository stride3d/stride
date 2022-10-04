using Spv;
using Spv.Generator;
using static Spv.Specification;

namespace SDSLParserExample;

public class TestModule : Module
{
    public TestModule() : base(Specification.Version) { }

            public void Construct()
            {
                AddCapability(Capability.Shader);
                SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);

                Instruction floatType = TypeFloat(32);
                Instruction floatInputType = TypePointer(StorageClass.Input, floatType);
                Instruction floatOutputType = TypePointer(StorageClass.Output, floatType);
                Instruction vec3Type = TypeVector(floatType, 3);
                Instruction vec4Type = TypeVector(floatType, 4);
                Instruction vec4InputPtrType = TypePointer(StorageClass.Input, vec4Type);
                Instruction vec3OutputPtrType = TypePointer(StorageClass.Output, vec3Type);
                Instruction vec4OutputPtrType = TypePointer(StorageClass.Output, vec4Type);
                Instruction vec4PositionPtrType = TypePointer(StorageClass.Output, vec4Type);

                Instruction inputPos = Variable(vec4InputPtrType, StorageClass.Input);
                // Decorate(inputPos, Decoration.Aliased, (LiteralInteger)0);
                // Instruction inputTest = Variable(floatInputType, StorageClass.Input);
                Instruction sv_pos = Variable(vec4PositionPtrType, StorageClass.Output);
                Decorate(sv_pos, Decoration.RelaxedPrecision);
                Decorate(sv_pos, Decoration.BuiltIn, (LiteralInteger)0);

                Instruction outputColor = Variable(vec4OutputPtrType, StorageClass.Output);
                // Decorate(outputColor, Decoration.);
                Decorate(outputColor, Decoration.Location, (LiteralInteger)111);

                Name(inputPos, "pos");
                Name(sv_pos, "svpos");
                Name(outputColor, "outputColor");
                AddGlobalVariable(inputPos);
                AddGlobalVariable(sv_pos);
                AddGlobalVariable(outputColor);

                Instruction rColor = Constant(floatType, 0.5f);
                Instruction gColor = Constant(floatType, 0.0f);
                Instruction bColor = Constant(floatType, 0.0f);
                Instruction aColor = Constant(floatType, 1.0f);

                Instruction compositeColor = ConstantComposite(vec4Type, rColor, gColor, bColor, aColor);

                Instruction voidType = TypeVoid();

                Instruction mainFunctionType = TypeFunction(voidType, true);
                Instruction mainFunction = Function(voidType, FunctionControlMask.MaskNone, mainFunctionType);
                AddLabel(Label());

                Instruction tempInput = Load(vec4Type, inputPos);

                Instruction resultSqrt = GlslSqrt(floatType, tempInput);

                Store(sv_pos, resultSqrt);
                Store(outputColor, compositeColor);

                Return();
                FunctionEnd();

                AddEntryPoint(ExecutionModel.Vertex, mainFunction, "main", inputPos, sv_pos, outputColor);
                // AddExecutionMode(mainFunction, ExecutionMode.OriginLowerLeft);
            }
}