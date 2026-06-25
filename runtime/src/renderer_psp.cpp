#include "psprpg/renderer.hpp"

#include <pspdisplay.h>
#include <pspgu.h>

namespace {

constexpr int buffer_width = 512;
constexpr int buffer_height = 272;

alignas(64) unsigned int display_list[32768];

struct Vertex {
    std::uint32_t color;
    float x;
    float y;
    float z;
};

} // namespace

namespace psprpg {

void renderer_initialize() {
    sceGuInit();
    sceGuStart(GU_DIRECT, display_list);
    sceGuDrawBuffer(GU_PSM_8888, reinterpret_cast<void*>(0), buffer_width);
    sceGuDispBuffer(screen_width, screen_height, reinterpret_cast<void*>(0x88000),
                    buffer_width);
    sceGuDepthBuffer(reinterpret_cast<void*>(0x110000), buffer_width);
    sceGuOffset(2048 - (screen_width / 2), 2048 - (screen_height / 2));
    sceGuViewport(2048, 2048, screen_width, screen_height);
    sceGuDepthRange(65535, 0);
    sceGuDisable(GU_DEPTH_TEST);
    sceGuEnable(GU_SCISSOR_TEST);
    sceGuScissor(0, 0, screen_width, screen_height);
    sceGuFinish();
    sceGuSync(0, 0);
    sceDisplayWaitVblankStart();
    sceGuDisplay(GU_TRUE);
}

void renderer_begin_frame(std::uint32_t clear_color) {
    sceGuStart(GU_DIRECT, display_list);
    sceGuClearColor(clear_color);
    sceGuClear(GU_COLOR_BUFFER_BIT);
}

void renderer_draw_rectangle(float x, float y, float width, float height,
                             std::uint32_t color) {
    auto* vertices =
        static_cast<Vertex*>(sceGuGetMemory(2 * sizeof(Vertex)));

    vertices[0] = Vertex{color, x, y, 0.0f};
    vertices[1] = Vertex{color, x + width, y + height, 0.0f};

    sceGuDrawArray(GU_SPRITES,
                   GU_COLOR_8888 | GU_VERTEX_32BITF | GU_TRANSFORM_2D,
                   2, nullptr, vertices);
}

void renderer_end_frame() {
    sceGuFinish();
    sceGuSync(0, 0);
    sceDisplayWaitVblankStart();
    sceGuSwapBuffers();
}

void renderer_shutdown() {
    sceGuDisplay(GU_FALSE);
    sceGuTerm();
}

} // namespace psprpg
