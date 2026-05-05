using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core;

/// <summary>
/// Can be parsed from SPIR-V words
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFromSpirv<T>
{
    static abstract T From(Span<int> words);
    static abstract T From(string value);
}
