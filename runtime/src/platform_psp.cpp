#include "psprpg/platform.hpp"

#include <pspkernel.h>

namespace {

volatile bool running = true;

int exit_callback(int, int, void*) {
    running = false;
    return 0;
}

int callback_thread(SceSize, void*) {
    const int callback_id =
        sceKernelCreateCallback("Exit Callback", exit_callback, nullptr);
    sceKernelRegisterExitCallback(callback_id);
    sceKernelSleepThreadCB();
    return 0;
}

} // namespace

namespace psprpg {

void platform_initialize() {
    const int thread_id = sceKernelCreateThread(
        "callback_thread", callback_thread, 0x11, 0xFA0, 0, nullptr);

    if (thread_id >= 0) {
        sceKernelStartThread(thread_id, 0, nullptr);
    }
}

bool platform_is_running() {
    return running;
}

void platform_shutdown() {
    sceKernelExitGame();
}

} // namespace psprpg

