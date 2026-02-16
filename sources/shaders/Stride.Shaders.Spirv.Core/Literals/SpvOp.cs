using System.Runtime.CompilerServices;
using System;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core;


// public record struct SpvEnum<T>(T Value) : IFromSpirv<SpvEnum<T>>
//     where T : Enum
// {
//     public static implicit operator T(SpvEnum<T> r) => r.Value;
//     public static implicit operator SpvEnum<T>(T v) => new(v);
//     public static SpvEnum<T> From(Span<int> words) => new() { Value = Unsafe.As<int, T>(ref words[0]) };

//     public static SpvEnum<T> From(string value)
//     {
//         throw new NotImplementedException();
//     }
// }