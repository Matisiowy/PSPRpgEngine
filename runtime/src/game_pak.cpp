#include "psprpg/game_pak.hpp"

#include <cstdio>
#include <cstring>

namespace {

constexpr std::uint32_t supported_pak_version = 1;
constexpr std::uint32_t scene_resource_type = 1;
constexpr std::size_t header_size = 48;
constexpr std::size_t entry_size = 32;
constexpr char pak_magic[8] = {'P', 'R', 'P', 'G', 'P', 'A', 'K', '\0'};

std::uint32_t read_u32(const std::uint8_t* bytes) {
    return static_cast<std::uint32_t>(bytes[0]) |
           (static_cast<std::uint32_t>(bytes[1]) << 8) |
           (static_cast<std::uint32_t>(bytes[2]) << 16) |
           (static_cast<std::uint32_t>(bytes[3]) << 24);
}

float read_float(const std::uint8_t* bytes) {
    const std::uint32_t value = read_u32(bytes);
    float result = 0.0f;
    std::memcpy(&result, &value, sizeof(result));
    return result;
}

bool read_exact(std::FILE* file, void* destination, std::size_t size) {
    return std::fread(destination, 1, size, file) == size;
}

} // namespace

namespace psprpg {

bool game_pak_load_startup_scene(const char* path, compiled_scene& scene) {
    std::FILE* file = std::fopen(path, "rb");
    if (file == nullptr) {
        return false;
    }

    std::uint8_t header[header_size];
    if (!read_exact(file, header, sizeof(header)) ||
        std::memcmp(header, pak_magic, sizeof(pak_magic)) != 0 ||
        read_u32(header + 8) != supported_pak_version) {
        std::fclose(file);
        return false;
    }

    const std::uint32_t entry_count = read_u32(header + 12);
    const std::uint32_t table_offset = read_u32(header + 16);
    const std::uint8_t* startup_id = header + 24;
    if (std::fseek(file, static_cast<long>(table_offset), SEEK_SET) != 0) {
        std::fclose(file);
        return false;
    }

    std::uint32_t scene_offset = 0;
    std::uint32_t scene_size = 0;
    std::uint8_t entry[entry_size];
    for (std::uint32_t index = 0; index < entry_count; ++index) {
        if (!read_exact(file, entry, sizeof(entry))) {
            std::fclose(file);
            return false;
        }
        if (read_u32(entry) == scene_resource_type &&
            std::memcmp(entry + 16, startup_id, 16) == 0) {
            scene_offset = read_u32(entry + 8);
            scene_size = read_u32(entry + 12);
            break;
        }
    }

    if (scene_offset == 0 || scene_size < 16 ||
        std::fseek(file, static_cast<long>(scene_offset), SEEK_SET) != 0) {
        std::fclose(file);
        return false;
    }

    std::uint8_t scene_header[16];
    if (!read_exact(file, scene_header, sizeof(scene_header)) ||
        read_u32(scene_header) != 1) {
        std::fclose(file);
        return false;
    }

    scene.width = read_u32(scene_header + 4);
    scene.height = read_u32(scene_header + 8);
    const std::uint32_t entity_count = read_u32(scene_header + 12);
    if (entity_count > max_scene_entities ||
        scene_size < 16 + entity_count * 40) {
        std::fclose(file);
        return false;
    }

    scene.entity_count = entity_count;
    std::uint8_t entity_data[40];
    for (std::size_t index = 0; index < scene.entity_count; ++index) {
        if (!read_exact(file, entity_data, sizeof(entity_data))) {
            std::fclose(file);
            return false;
        }

        scene_entity& entity = scene.entities[index];
        std::memcpy(entity.id, entity_data, 16);
        entity.x = read_float(entity_data + 16);
        entity.y = read_float(entity_data + 20);
        entity.width = read_float(entity_data + 24);
        entity.height = read_float(entity_data + 28);
        entity.color = read_u32(entity_data + 32);
        entity.flags = read_u32(entity_data + 36);
    }

    std::fclose(file);
    return true;
}

} // namespace psprpg
