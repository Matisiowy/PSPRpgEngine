cmake_minimum_required(VERSION 3.11)

project(psp_rpg_runtime LANGUAGES CXX)

add_executable(${PROJECT_NAME}
    src/main.cpp
    src/game_pak.cpp
    src/platform_psp.cpp
    src/renderer_psp.cpp
)

target_include_directories(${PROJECT_NAME} PRIVATE include)
target_compile_features(${PROJECT_NAME} PRIVATE cxx_std_17)
target_compile_options(${PROJECT_NAME} PRIVATE -Wall -Wextra -fno-exceptions -fno-rtti)

target_link_libraries(${PROJECT_NAME} PRIVATE
    pspgu
    pspge
    pspdisplay
)

create_pbp_file(
    TARGET ${PROJECT_NAME}
    ICON_PATH NULL
    BACKGROUND_PATH NULL
    PREVIEW_PATH NULL
    TITLE "PSP RPG Engine"
    VERSION 01.00
)
