project(DxtWrapper)
cmake_minimum_required(VERSION 2.6)
file(GLOB SRCS *.cpp *.h)

find_package(directxtex CONFIG REQUIRED)

add_library(${PROJECT_NAME} SHARED ${SRCS} )

target_link_libraries(${PROJECT_NAME} PRIVATE Microsoft::DirectXTex)