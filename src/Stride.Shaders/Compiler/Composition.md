# Design for generating spirv

A shader will be described as different kinds : 

* A single shader
* An array of shader to compose as one shader
* A mixin shader for effect composition

## Single Shader

A single shader should be a full shader, defining all methods and variables in one single shader object (no mixins)

## Array shader

An array of shader will contain multiple shader definition all linked by inheritances. It can be created from one shader requiring parent shaders to find in a shader dictionary/storage of some sort.

## Mixin graph

The graph that the mixin system forms will have to be simplified to an array shader.

### Spirv design

#### RGroup and CBuffer

Each members of both will be queried from all shadercodes, merged together to form a fuller version of both rgroup and cbuffer.

#### Static methods and members

Methods and members that are marked as static/staged will be generated once (with a check on duplicates) as spirv methods.
Temporary IDs will be generated (maybe GuID?) and later converted to actual available IDs for the spirv module.

#### NonStatic/Inherited members, stream values...

Needs a bit of research.
Instead of generating full methods, we generate a list of statements for each methods then combine them depending the order.

