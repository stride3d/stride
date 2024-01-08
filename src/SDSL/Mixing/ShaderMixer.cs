using SDSL.Parsing.AST.Shader;
using SoftTouch.Spirv;
using static Spv.Specification;

namespace SDSL.Mixing;

public static class ShaderMixer
{
    public static void Compile(ShaderProgram program)
    {
        var mixer = new Mixer(program.Name);

        foreach(var method in program.Body.OfType<ModuleMethod>())
        {
            var function = mixer.WithFunction(
                method.ReturnType.Name, 
                method.Name,
                (builder) => {
                    if(method.ParameterList != null)
                    foreach(var p in method.ParameterList)
                        builder.With(p.Type.Name, p.Name);
                    return builder;
                }
            );
            function.FunctionEnd();
        }
        foreach (var method in program.Body.OfType<ShaderMethod>())
        {
            if(method is VSMainMethod vs)
            {
                var func = mixer
                    .WithEntryPoint(
                        ExecutionModel.Vertex,
                        vs.Name
                    )
                    .FunctionStart();
                
                func.FunctionEnd();
            }
        }
        mixer.Build();
    }
}
