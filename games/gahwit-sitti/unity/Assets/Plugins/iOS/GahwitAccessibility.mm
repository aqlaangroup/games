// The phone's own accessibility settings, for Boot.PhoneSettings.
#import <UIKit/UIKit.h>

// 1 when Reduce Motion is on. An int, so the value crosses to C# the same way on every compiler.
extern "C" int GahwitReduceMotion()
{
    return UIAccessibilityIsReduceMotionEnabled() ? 1 : 0;
}
