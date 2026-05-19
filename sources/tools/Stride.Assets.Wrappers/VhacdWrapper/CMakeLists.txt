# Built and packaged by .github/workflows/dep-vhacd.yml.
# Wraps kmammou/v-hacd v4 (single-header library) into a flat C ABI shared lib
# consumed by Stride.Assets at asset-build time for convex hull decomposition.
#
# Workflow passes:
#   -DVHACD_SRC = cloned kmammou/v-hacd source tree

cmake_minimum_required(VERSION 3.21)
project(stride_vhacd LANGUAGES CXX)

if(NOT DEFINED VHACD_SRC)
    message(FATAL_ERROR "Set -DVHACD_SRC to the cloned kmammou/v-hacd source tree.")
endif()

add_library(stride_vhacd SHARED vhacd_wrapper.cpp)
set_target_properties(stride_vhacd PROPERTIES
    PREFIX ""
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    POSITION_INDEPENDENT_CODE ON
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON)

target_include_directories(stride_vhacd PRIVATE "${VHACD_SRC}/include")
