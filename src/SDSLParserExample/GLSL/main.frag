#version 450


layout (location = 0) in vec4 color;
layout (location = 1) in vec3 pos;
layout (location = 0) out vec4 o_color;
  

struct Stream {
    vec4 col;
    vec3 pos;
};


Stream streams;


vec4 ComputeColor(inout Stream streams)
{
    streams.col = color;
    return streams.col;
}

void main()
{
    streams.pos = pos;
    vec4 color2 = ComputeColor(streams);
    o_color = color;
}
