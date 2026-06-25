#include "psprpg/platform.hpp"
#include "psprpg/renderer.hpp"

#include <pspkernel.h>

#include <cstdint>

PSP_MODULE_INFO("PSP RPG Engine", 0, 1, 0);
PSP_MAIN_THREAD_ATTR(PSP_THREAD_ATTR_USER | PSP_THREAD_ATTR_VFPU);

int main() {
    psprpg::platform_initialize();
    psprpg::renderer_initialize();

    while (psprpg::platform_is_running()) {
        psprpg::renderer_begin_frame(0xFF201810);

        // M0 test scene. In M1 these values will come from compiled project data.
        psprpg::renderer_draw_rectangle(0.0f, 216.0f, 480.0f, 56.0f, 0xFF345F34);
        psprpg::renderer_draw_rectangle(224.0f, 120.0f, 32.0f, 32.0f, 0xFFFFA64D);

        psprpg::renderer_end_frame();
    }

    psprpg::renderer_shutdown();
    psprpg::platform_shutdown();
    return 0;
}
