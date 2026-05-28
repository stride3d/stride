# Bundles SPIRV-Tools-static + SPIRV-Tools-opt + the C shim into a single
# shared library, stride_spirv_tools, re-exporting both the upstream libspirv.h
# C API and the shim's stride_spv* entry points.
#
# Usage (from dep-spirv-tools.yml):
#   cmake -S build/deps/spirv-tools -B _build \
#         -DSPIRV_TOOLS_SOURCE_DIR=<path to cloned SPIRV-Tools>
#   cmake --build _build

cmake_minimum_required(VERSION 3.15)
project(stride_spirv_tools LANGUAGES C CXX)

if(NOT DEFINED SPIRV_TOOLS_SOURCE_DIR)
    message(FATAL_ERROR "Set -DSPIRV_TOOLS_SOURCE_DIR to the cloned SPIRV-Tools source tree.")
endif()

# Force the upstream build to produce static archives we can absorb.
set(BUILD_SHARED_LIBS   OFF CACHE BOOL "" FORCE)
set(SPIRV_SKIP_TESTS    ON  CACHE BOOL "" FORCE)
set(SPIRV_SKIP_EXECUTABLES ON CACHE BOOL "" FORCE)
set(SPIRV_WERROR        OFF CACHE BOOL "" FORCE)

# SPIRV-Tools' SPIRV_TOOLS_EXPORT macro only activates when SPIRV_TOOLS_SHAREDLIB
# is defined; SPIRV_TOOLS_IMPLEMENTATION then flips it from dllimport to dllexport.
# Both must be defined when compiling the static libs so their C API symbols get
# __declspec(dllexport) and end up in our shared output's export table.
add_compile_definitions(SPIRV_TOOLS_IMPLEMENTATION SPIRV_TOOLS_SHAREDLIB)

add_subdirectory(${SPIRV_TOOLS_SOURCE_DIR} spirv-tools-build EXCLUDE_FROM_ALL)

add_library(stride_spirv_tools SHARED stride_spvopt_shim.cpp)
set_target_properties(stride_spirv_tools PROPERTIES
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    POSITION_INDEPENDENT_CODE ON
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON)

target_include_directories(stride_spirv_tools PRIVATE
    ${SPIRV_TOOLS_SOURCE_DIR}/include)

# Whole-archive both static libs so no public symbol gets dropped by dead-code
# stripping. The SPIRV_TOOLS_EXPORT/STRIDE_SPV_EXPORT attributes drive what's
# actually exposed in the final symbol table.
if(WIN32)
    target_link_options(stride_spirv_tools PRIVATE
        /WHOLEARCHIVE:$<TARGET_FILE_NAME:SPIRV-Tools-static>
        /WHOLEARCHIVE:$<TARGET_FILE_NAME:SPIRV-Tools-opt>)
    target_link_libraries(stride_spirv_tools PRIVATE SPIRV-Tools-static SPIRV-Tools-opt)
elseif(APPLE)
    target_link_libraries(stride_spirv_tools PRIVATE
        -Wl,-force_load $<TARGET_FILE:SPIRV-Tools-static>
        -Wl,-force_load $<TARGET_FILE:SPIRV-Tools-opt>)
else()
    target_link_libraries(stride_spirv_tools PRIVATE
        -Wl,--whole-archive SPIRV-Tools-static SPIRV-Tools-opt -Wl,--no-whole-archive)
endif()
