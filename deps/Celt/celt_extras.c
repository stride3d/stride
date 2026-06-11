// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Stride-specific helpers compiled into libCelt.a alongside the upstream Opus/Celt sources.
// Each function here is a non-variadic wrapper around a variadic opus_custom_decoder_ctl call,
// so .NET DllImport can invoke them from C# (Mono on Apple does not support __arglist for
// DllImport, and the Apple ARM64 variadic ABI passes overflow args on the stack — a plain
// non-variadic DllImport would route them through registers and corrupt memory).

#include <stdint.h>
#include "opus_custom.h"

// Returns the decoder's lookahead (encoder-delay) sample count via *out_delay.
// OPUS_GET_LOOKAHEAD's macro arg is itself the int* (it pipes through opus_check_int_ptr).
int stride_celt_get_lookahead(OpusCustomDecoder *decoder, int32_t *out_delay)
{
    return opus_custom_decoder_ctl(decoder, OPUS_GET_LOOKAHEAD(out_delay));
}
