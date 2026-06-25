#pragma once

#include <cstddef>
#include <cstdint>

namespace psprpg {

constexpr std::size_t max_scene_entities = 256;

struct scene_entity {
    std::uint8_t id[16];
    float x;
    float y;
    float width;
    float height;
    std::uint32_t color;
    std::uint32_t flags;
};

struct compiled_scene {
    std::uint32_t width;
    std::uint32_t height;
    std::size_t entity_count;
    scene_entity entities[max_scene_entities];
};

bool game_pak_load_startup_scene(const char* path, compiled_scene& scene);

} // namespace psprpg
