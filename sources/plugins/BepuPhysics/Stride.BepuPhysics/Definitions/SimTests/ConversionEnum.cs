// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.BepuPhysics.Definitions.SimTests;

public readonly ref struct ConversionEnum<TConverter, TSource, TDest>(Span<TSource> span, TConverter converter) where TConverter : IConverter<TSource, TDest> where TSource : unmanaged
{
    public readonly Span<TSource> Span = span; // Workaround to enable passing ref types
    public Enumerator GetEnumerator() => new(Span, converter);

    public ref struct Enumerator
    {
        private int _index;
        private readonly Span<TSource> _sources;
        private TDest _current;
        private TConverter _converter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(Span<TSource> sources, TConverter converter)
        {
            _sources = sources;
            _current = default!;
            _converter = converter;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            do
            {
                int nextIndex = _index + 1;
                if (nextIndex < _sources.Length)
                {
                    if (_converter.TryConvert(_sources[nextIndex], out _current) == false)
                        continue;
                    _index = nextIndex;
                    return true;
                }

                return false;
            } while (true);
        }

        public TDest Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}
