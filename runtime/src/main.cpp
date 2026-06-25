#include "psprpg/game_pak.hpp"
#include "psprpg/platform.hpp"
#include "psprpg/renderer.hpp"

#include <pspkernel.h>

#include <cstdint>
#include <cstdio>
#include <cstring>

PSP_MODULE_INFO("PSP RPG Engine", 0, 1, 0);
PSP_MAIN_THREAD_ATTR(PSP_THREAD_ATTR_USER | PSP_THREAD_ATTR_VFPU);

namespace {

void make_game_pak_path(const char* executable_path, char* output,
                        std::size_t output_size) {
    std::snprintf(output, output_size, "game.pak");
    if (executable_path == nullptr) {
        return;
    }

    const char* slash = std::strrchr(executable_path, '/');
    if (slash == nullptr) {
        return;
    }

    const std::size_t directory_length =
        static_cast<std::size_t>(slash - executable_path + 1);
    if (directory_length + 8 >= output_size) {
        return;
    }

    std::memcpy(output, executable_path, directory_length);
    std::memcpy(output + directory_length, "game.pak", 9);
}

} // namespace

int main(int argc, char** argv) {
    psprpg::platform_initialize();
    psprpg::renderer_initialize();

    char game_pak_path[256];
    make_game_pak_path(argc > 0 ? argv[0] : nullptr,
                       game_pak_path, sizeof(game_pak_path));
    psprpg::compiled_scene scene{};
    const bool scene_loaded =
        psprpg::game_pak_load_startup_scene(game_pak_path, scene);

    while (psprpg::platform_is_running()) {
        psprpg::renderer_begin_frame(0xFF201810);

        if (scene_loaded) {
            for (std::size_t index = 0; index < scene.entity_count; ++index) {
                const psprpg::scene_entity& entity = scene.entities[index];
                if ((entity.flags & 1U) != 0U) {
                    psprpg::renderer_draw_rectangle(
                        entity.x, entity.y, entity.width, entity.height,
                        entity.color);
                }
            }
        } else {
            // Magenta means game.pak could not be loaded.
            psprpg::renderer_draw_rectangle(
                208.0f, 104.0f, 64.0f, 64.0f, 0xFFFF00FF);
        }

        psprpg::renderer_end_frame();
    }

    psprpg::renderer_shutdown();
    psprpg::platform_shutdown();
    return 0;
}
