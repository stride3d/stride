# Built and packaged by .github/workflows/dep-msdfgen.yml.
# Static-links msdfgen-core + msdfgen-ext (and FreeType) into a single
# stride_msdfgen.{dll,so,dylib} per RID.
#
# Workflow passes:
#   -DMSDFGEN_SRC  = cloned Chlumsky/msdfgen
# and installs FreeType to a staging prefix surfaced via CMAKE_PREFIX_PATH.

cmake_minimum_required(VERSION 3.21)
project(stride_msdfgen LANGUAGES CXX)

if(NOT DEFINED MSDFGEN_SRC)
    message(FATAL_ERROR "Set -DMSDFGEN_SRC to the cloned msdfgen source tree.")
endif()

# Force msdfgen subprojects to static libs we can absorb, and skip everything we don't use.
set(BUILD_SHARED_LIBS         OFF CACHE BOOL "" FORCE)
set(MSDFGEN_BUILD_STANDALONE  OFF CACHE BOOL "" FORCE)
set(MSDFGEN_INSTALL           OFF CACHE BOOL "" FORCE)
set(MSDFGEN_USE_VCPKG         OFF CACHE BOOL "" FORCE)
set(MSDFGEN_USE_SKIA          OFF CACHE BOOL "" FORCE)
set(MSDFGEN_DISABLE_SVG       ON  CACHE BOOL "" FORCE)
set(MSDFGEN_DISABLE_PNG       ON  CACHE BOOL "" FORCE)
set(MSDFGEN_CORE_ONLY         OFF CACHE BOOL "" FORCE)
set(MSDFGEN_DYNAMIC_RUNTIME   ON  CACHE BOOL "" FORCE)

add_subdirectory(${MSDFGEN_SRC} msdfgen-build EXCLUDE_FROM_ALL)

add_library(stride_msdfgen SHARED msdfgen_wrapper.cpp)
set_target_properties(stride_msdfgen PROPERTIES
    PREFIX ""
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    POSITION_INDEPENDENT_CODE ON
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON)

target_link_libraries(stride_msdfgen PRIVATE msdfgen::msdfgen msdfgen::msdfgen-ext)
