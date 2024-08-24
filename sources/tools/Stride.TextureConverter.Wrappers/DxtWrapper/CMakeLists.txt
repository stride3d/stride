project(DxtWrapper)
cmake_minimum_required(VERSION 3.10)
file(GLOB SRCS *.cpp *.h)

set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ../../../../../deps/TextureWrappers/Release/linux-x64)

find_package(directxtex CONFIG REQUIRED)

add_library(${PROJECT_NAME} SHARED ${SRCS} )
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")

target_link_libraries(${PROJECT_NAME} PRIVATE Microsoft::DirectXTex)