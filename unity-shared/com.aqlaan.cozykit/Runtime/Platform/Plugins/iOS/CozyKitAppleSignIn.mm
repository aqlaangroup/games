// Sign in with Apple for Aqlaan.CozyKit.Platform.AppleSignIn. No name or email is requested.
#import <AuthenticationServices/AuthenticationServices.h>

extern "C" UIWindow* UnityGetMainWindow(void);

typedef void (*CozyKitAppleCallback)(const char* idToken, const char* error);

API_AVAILABLE(ios(13.0))
@interface CozyKitAppleSignInDelegate : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@property (nonatomic, assign) CozyKitAppleCallback callback;
@property (nonatomic, strong) ASAuthorizationController* controller;
@end

static id cozyKitAppleDelegate; // kept alive until Apple answers

@implementation CozyKitAppleSignInDelegate

- (void)authorizationController:(ASAuthorizationController*)controller didCompleteWithAuthorization:(ASAuthorization*)authorization
{
    NSString* token = nil;
    if ([authorization.credential isKindOfClass:[ASAuthorizationAppleIDCredential class]])
    {
        NSData* data = ((ASAuthorizationAppleIDCredential*)authorization.credential).identityToken;
        if (data != nil) token = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    }
    [self finish:token error:token == nil ? @"no_token" : nil];
}

- (void)authorizationController:(ASAuthorizationController*)controller didCompleteWithError:(NSError*)error
{
    [self finish:nil error:error.code == ASAuthorizationErrorCanceled ? @"canceled" : error.localizedDescription];
}

- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController*)controller
{
    return UnityGetMainWindow();
}

- (void)finish:(NSString*)token error:(NSString*)error
{
    CozyKitAppleCallback callback = self.callback;
    cozyKitAppleDelegate = nil;
    if (callback != NULL) callback(token != nil ? token.UTF8String : NULL, error != nil ? error.UTF8String : NULL);
}

@end

extern "C" void CozyKitAppleSignIn(const char* hashedNonce, CozyKitAppleCallback callback)
{
    if (@available(iOS 13.0, *))
    {
        ASAuthorizationAppleIDRequest* request = [[[ASAuthorizationAppleIDProvider alloc] init] createRequest];
        request.requestedScopes = @[];
        request.nonce = [NSString stringWithUTF8String:hashedNonce];

        CozyKitAppleSignInDelegate* delegate = [[CozyKitAppleSignInDelegate alloc] init];
        delegate.callback = callback;
        delegate.controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        delegate.controller.delegate = delegate;
        delegate.controller.presentationContextProvider = delegate;
        cozyKitAppleDelegate = delegate;
        [delegate.controller performRequests];
    }
    else if (callback != NULL)
    {
        callback(NULL, "unsupported");
    }
}
