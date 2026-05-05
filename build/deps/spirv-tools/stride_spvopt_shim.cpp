// C-callable wrappers around spvtools::Optimizer. Upstream SPIRV-Tools exposes
// the optimizer only via optimizer.hpp (C++), which P/Invoke can't reach
// because of name mangling. This shim compiles into stride_spirv_tools and
// exports stable C entry points that the managed bindings call.

#include <spirv-tools/optimizer.hpp>
#include <spirv-tools/libspirv.h>
#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <vector>

#if defined(_WIN32)
    #define STRIDE_SPV_EXPORT __declspec(dllexport)
#else
    #define STRIDE_SPV_EXPORT __attribute__((visibility("default")))
#endif

extern "C" {

STRIDE_SPV_EXPORT void* stride_spvOptimizerCreate(spv_target_env env) {
    return static_cast<void*>(new spvtools::Optimizer(env));
}

STRIDE_SPV_EXPORT void stride_spvOptimizerDestroy(void* optimizer) {
    delete static_cast<spvtools::Optimizer*>(optimizer);
}

STRIDE_SPV_EXPORT void stride_spvOptimizerRegisterLegalizationPasses(void* optimizer) {
    static_cast<spvtools::Optimizer*>(optimizer)->RegisterLegalizationPasses();
}

// Same legalization recipe, but with preserve_interface=true so that unused
// Input/Output variables aren't stripped. Needed because Stride runs legalize
// on a merged multi-stage module: dropping an Output in the predecessor stage
// while keeping an Input with the same Location in the successor leaves FXC
// assigning mismatched hardware registers to the same semantic, which D3D11's
// runtime signature check then rejects ("Semantic X defined for mismatched
// hardware registers"). See EffectCompiler.cs for the full picture.
STRIDE_SPV_EXPORT void stride_spvOptimizerRegisterLegalizationPassesPreserveInterface(void* optimizer) {
    static_cast<spvtools::Optimizer*>(optimizer)->RegisterLegalizationPasses(true);
}

STRIDE_SPV_EXPORT void stride_spvOptimizerRegisterPerformancePasses(void* optimizer) {
    static_cast<spvtools::Optimizer*>(optimizer)->RegisterPerformancePasses();
}

// Register a single pass by its spirv-opt CLI flag (e.g. "--eliminate-dead-code-aggressive",
// "--scalar-replacement=0"). Returns non-zero on success. Lets managed code assemble
// custom pass pipelines without requiring a new shim export per recipe.
STRIDE_SPV_EXPORT int stride_spvOptimizerRegisterPassFromFlag(
    void* optimizer, const char* flag, int preserve_interface) {
    return static_cast<spvtools::Optimizer*>(optimizer)
        ->RegisterPassFromFlag(flag, preserve_interface != 0) ? 1 : 0;
}

STRIDE_SPV_EXPORT spv_result_t stride_spvOptimizerRun(
    void* optimizer,
    const uint32_t* input_binary, size_t input_word_count,
    uint32_t** output_binary, size_t* output_word_count) {

    auto* opt = static_cast<spvtools::Optimizer*>(optimizer);
    std::vector<uint32_t> result;
    if (!opt->Run(input_binary, input_word_count, &result)) {
        *output_binary = nullptr;
        *output_word_count = 0;
        return SPV_ERROR_INTERNAL;
    }
    // Transfer ownership to caller via malloc so the managed side can pair it
    // with stride_spvOptimizerFreeBinary (std::free) — no CRT boundary risk.
    const size_t byte_count = result.size() * sizeof(uint32_t);
    auto* buffer = static_cast<uint32_t*>(std::malloc(byte_count));
    if (!buffer) {
        *output_binary = nullptr;
        *output_word_count = 0;
        return SPV_ERROR_OUT_OF_MEMORY;
    }
    std::memcpy(buffer, result.data(), byte_count);
    *output_binary = buffer;
    *output_word_count = result.size();
    return SPV_SUCCESS;
}

STRIDE_SPV_EXPORT void stride_spvOptimizerFreeBinary(uint32_t* binary) {
    std::free(binary);
}

} // extern "C"
