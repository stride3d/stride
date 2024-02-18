cmake_minimum_required(VERSION 3.26)
project(DxtWrapper)
file(GLOB SRCS *.cpp *.h)

find_package(directxtex CONFIG REQUIRED)

add_library(${PROJECT_NAME} SHARED ${SRCS} )

target_link_libraries(${PROJECT_NAME} PRIVATE Microsoft::DirectXTex)

set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")