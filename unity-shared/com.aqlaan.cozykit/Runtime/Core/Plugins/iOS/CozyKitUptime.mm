#include <time.h>

// Milliseconds since the phone started. On Apple platforms CLOCK_MONOTONIC keeps counting while the phone sleeps.
extern "C" long long CozyKitUptimeMs()
{
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (long long)ts.tv_sec * 1000 + ts.tv_nsec / 1000000;
}
