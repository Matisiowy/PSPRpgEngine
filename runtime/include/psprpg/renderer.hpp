#pragma once

#include <cstdint>

namespace psprpg {

constexpr int screen_width = 480;
constexpr int screen_height = 272;

void renderer_initialize();
void renderer_begin_frame(std::uint32_t clear_color);
void renderer_draw_rectangle(float x, float y, float width, float height,
                             std::uint32_t color);
void renderer_end_frame();
void renderer_shutdown();

} // namespace psprpg

