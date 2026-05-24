# Built and packaged by .github/workflows/dep-dxtwrapper.yml.
# Static-links DirectXTex into a single stride_directxtex.{dll,so,dylib} per RID.
#
# Workflow passes:
#   -DDIRECTXTEX_SRC = cloned microsoft/DirectXTex
#
# On non-Windows, DirectXTex's CMake does find_package(directxmath / directx-headers)
# unconditionally, so the workflow installs both deps to a staging prefix and points
# us at it via CMAKE_PREFIX_PATH.

cmake_minimum_required(VERSION 3.21)
project(stride_directxtex LANGUAGES CXX)

if(NOT DEFINED DIRECTXTEX_SRC)
    message(FATAL_ERROR "Set -DDIRECTXTEX_SRC to the cloned DirectXTex source tree.")
endif()

# Force DirectXTex to a static lib we can absorb, and skip everything we don't use.
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(BUILD_TOOLS       OFF CACHE BOOL "" FORCE)
set(BUILD_SAMPLE      OFF CACHE BOOL "" FORCE)
set(BUILD_DX11        OFF CACHE BOOL "" FORCE)
set(BUILD_DX12        OFF CACHE BOOL "" FORCE)

add_subdirectory(${DIRECTXTEX_SRC} directxtex-build EXCLUDE_FROM_ALL)

add_library(stride_directxtex SHARED dxt_wrapper.cpp)
set_target_properties(stride_directxtex PROPERTIES
    PREFIX ""
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    POSITION_INDEPENDENT_CODE ON
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON)

target_link_libraries(stride_directxtex PRIVATE DirectXTex)
