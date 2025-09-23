using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Stride.Graphics.RHI;



public class OpenGLFrameRenderer(uint width = 800, uint height = 600, byte[]? fragmentSpirv = null, byte[]? vertexSpirv = null) : FrameRenderer(width, height, vertexSpirv, fragmentSpirv)
{
    static IWindow? window;
    static GL? Gl;

    uint width = width;
    uint height = height;

    uint Fbo;
    uint FboTex;
    uint Vbo;
    uint Ebo;
    uint Vao;
    uint Shader;

    byte[]? fragmentSpirv = fragmentSpirv;

    //Vertex shaders are run on each vertex.
    private static readonly string VertexShaderSource = @"
        #version 330 core //Using version GLSL version 3.3
        layout (location = 0) in vec4 vPos;
        
        void main()
        {
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

    //Fragment shaders are run on each fragment/pixel of the geometry.
    private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        }
        ";

    //Vertex data, uploaded to the VBO.
    private static readonly float[] Vertices =
    [
        //X    Y      Z
        1f,  1f, 0f,
        1f, -1f, 0f,
        -1f,-1f, 0f,
        -1f, 1f, 1f
    ];

    //Index data, uploaded to the EBO.
    private static readonly uint[] Indices =
    [
            0, 1, 3,
            1, 2, 3
    ];


    public override unsafe void RenderFrame(Span<byte> result)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>((int)width, (int)height);
        options.IsVisible = false;
        options.ShouldSwapAutomatically = false;
        window = Window.Create(options);
        window.Initialize();
        //Getting the opengl api for drawing to the screen.
        Gl = GL.GetApi(window);

        // Generate a FBO


        Gl.GenFramebuffers(1, out Fbo);
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, Fbo);

        Gl.GenTextures(1, out FboTex);
        Gl.BindTexture(TextureTarget.Texture2D, FboTex);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, FboTex, 0);



        //Creating a vertex array.
        Vao = Gl.GenVertexArray();
        Gl.BindVertexArray(Vao);

        //Initializing a vertex buffer that holds the vertex data.
        Vbo = Gl.GenBuffer(); //Creating the buffer.
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo); //Binding the buffer.
        fixed (void* v = &Vertices[0])
        {
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(uint)), v, BufferUsageARB.StaticDraw); //Setting buffer data.
        }

        //Initializing a element buffer that holds the index data.
        Ebo = Gl.GenBuffer(); //Creating the buffer.
        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo); //Binding the buffer.
        fixed (void* i = &Indices[0])
        {
            Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw); //Setting buffer data.
        }

        //Creating a vertex shader.
        uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
        Gl.ShaderSource(vertexShader, VertexShaderSource);
        Gl.CompileShader(vertexShader);

        //Checking the shader for compilation errors.
        string infoLog = Gl.GetShaderInfoLog(vertexShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Error compiling vertex shader {infoLog}");
        }

        //Creating a fragment shader.
        uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
        if (fragmentSpirv is not null)
        {
            unsafe
            {
                fixed (byte* spirv = fragmentSpirv)
                    Gl.ShaderBinary([fragmentShader], GLEnum.SpirVBinary, (void*)spirv, (uint)fragmentSpirv.Length);
            }
        }
        else
        {
            Gl.ShaderSource(fragmentShader, FragmentShaderSource);
            Gl.CompileShader(fragmentShader);
        }

        //Checking the shader for compilation errors.
        infoLog = Gl.GetShaderInfoLog(fragmentShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Error compiling fragment shader {infoLog}");
        }

        //Combining the shaders under one shader program.
        Shader = Gl.CreateProgram();
        Gl.AttachShader(Shader, vertexShader);
        Gl.AttachShader(Shader, fragmentShader);
        Gl.LinkProgram(Shader);

        //Checking the linking for errors.
        Gl.GetProgram(Shader, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(Shader)}");
        }

        //Delete the no longer useful individual shaders;
        Gl.DetachShader(Shader, vertexShader);
        Gl.DetachShader(Shader, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        //Tell opengl how to give the data to the shaders.
        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        Gl.EnableVertexAttribArray(0);

        // Just render once
        Gl.Clear((uint)ClearBufferMask.ColorBufferBit);

        //Bind the geometry and shader.
        Gl.BindVertexArray(Vao);
        Gl.UseProgram(Shader);

        //Draw the geometry.
        Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);

        Gl.ReadPixels(0, 0, width, height, GLEnum.Rgba, GLEnum.UnsignedByte, result);
        window?.Close();
        window?.Dispose();

    }
}