# Parser for SDSL

`Warn : this project is not official`

## SDSL

[SDSL](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/shading-language/index.html) is a shader language created for the [Stride game engine](https://www.stride3d.net/).

SDSL is a superset of the HLSL Shading language, bringing advanced and higher level language constructions, with:

* **extensibility** to allow shaders to be extended easily using object-oriented programming concepts such as classes, inheritance, and composition

* **modularity** to provide a set modular shaders each focusing on a single rendering technique, more easily manageable

* **reusability** to maximize code reuse between shaders


## Parser

This language parser is built from the ground up using the [Eto.Parse](https://github.com/picoe/Eto.Parse) library. It is designed to be faster and more efficient.