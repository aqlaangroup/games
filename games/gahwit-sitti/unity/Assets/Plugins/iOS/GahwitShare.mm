// The share sheet for postcards (Boot.ShareSheet): an image file and a line of text.
#import <UIKit/UIKit.h>

extern UIViewController* UnityGetGLViewController();

extern "C" void GahwitShare(const char* path, const char* text)
{
    NSMutableArray* items = [NSMutableArray array];
    UIImage* image = [UIImage imageWithContentsOfFile:[NSString stringWithUTF8String:path]];
    if (image != nil) [items addObject:image];
    if (text != NULL && strlen(text) > 0) [items addObject:[NSString stringWithUTF8String:text]];
    if (items.count == 0) return;

    UIActivityViewController* share = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
    UIViewController* root = UnityGetGLViewController();
    // On an iPad the sheet is a popover and needs a place to point at.
    share.popoverPresentationController.sourceView = root.view;
    share.popoverPresentationController.sourceRect = CGRectMake(root.view.bounds.size.width / 2, root.view.bounds.size.height / 2, 1, 1);
    [root presentViewController:share animated:YES completion:nil];
}
