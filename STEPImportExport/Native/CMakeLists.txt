cmake_minimum_required(VERSION 3.20)
project(tvgl_step_bridge LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(NOT DEFINED OCCT_DIR OR "${OCCT_DIR}" STREQUAL "")
    if(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/occt/include")
        set(OCCT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/occt")
    elseif(DEFINED ENV{USERPROFILE} AND EXISTS "$ENV{USERPROFILE}/.nuget/packages/occt.win64/7.8.1.3/build/include")
        set(OCCT_DIR "$ENV{USERPROFILE}/.nuget/packages/occt.win64/7.8.1.3/build")
    endif()
endif()

include_directories(
    "${OCCT_DIR}/include"
    "${CMAKE_CURRENT_SOURCE_DIR}"
)

link_directories("${OCCT_DIR}/lib")

add_library(tvgl_step_bridge SHARED
    tvgl_step_bridge.h
    tvgl_step_bridge.cpp
)

target_compile_definitions(tvgl_step_bridge PRIVATE 
    TVGL_STEP_BRIDGE_EXPORTS 
    WNT 
    _CRT_SECURE_NO_WARNINGS
)

target_link_libraries(tvgl_step_bridge PRIVATE
    TKDESTEP
    TKDE
    TKXSBase
    TKMesh
    TKTopAlgo
    TKBRep
    TKGeomBase
    TKG3d
    TKG2d
    TKMath
    TKernel
    TKShHealing
    TKXCAF
    TKLCAF
    TKPrim
    TKBool
    TKBO
)

# Output build artifacts
set_target_properties(tvgl_step_bridge PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/win-x64/native"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/win-x64/native"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/win-x64/native"
)
