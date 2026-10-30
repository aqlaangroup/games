// Light taps for Boot.AudioService.Buzz: 0 light (merge), 1 medium (serve), 2 success (level up).
#import <UIKit/UIKit.h>

extern "C" void GahwitHaptic(int style)
{
    if (style == 2)
    {
        [[[UINotificationFeedbackGenerator alloc] init] notificationOccurred:UINotificationFeedbackTypeSuccess];
        return;
    }
    UIImpactFeedbackStyle impact = style == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleLight;
    [[[UIImpactFeedbackGenerator alloc] initWithStyle:impact] impactOccurred];
}
