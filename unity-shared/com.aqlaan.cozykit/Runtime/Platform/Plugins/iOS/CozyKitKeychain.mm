// Keychain storage for Aqlaan.CozyKit.Platform.DeviceSecrets. Items stay on this phone (no iCloud sync) and usually outlive a reinstall.
#import <Foundation/Foundation.h>
#import <Security/Security.h>

// Each app keeps its items under its own bundle id. Gahwit Sitti's first builds used a fixed name, still read
// (and cleared) so a phone that had them keeps its account.
static NSString* const LegacyService = @"com.aqlaan.gahwit.platform";

static NSMutableDictionary* CozyKitKeychainQuery(const char* key, NSString* service)
{
    return [@{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: service,
        (__bridge id)kSecAttrAccount: [NSString stringWithUTF8String:key],
    } mutableCopy];
}

static NSString* CozyKitService()
{
    return [[[NSBundle mainBundle] bundleIdentifier] stringByAppendingString:@".platform"];
}

static NSString* CozyKitKeychainRead(const char* key, NSString* service)
{
    NSMutableDictionary* query = CozyKitKeychainQuery(key, service);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef result = NULL;
    if (SecItemCopyMatching((__bridge CFDictionaryRef)query, &result) != errSecSuccess || result == NULL) return nil;
    NSData* data = (__bridge_transfer NSData*)result;
    return [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
}

extern "C" char* CozyKitKeychainGet(const char* key)
{
    NSString* value = CozyKitKeychainRead(key, CozyKitService());
    if (value == nil) value = CozyKitKeychainRead(key, LegacyService);
    return value != nil ? strdup(value.UTF8String) : NULL; // the managed side frees it
}

extern "C" void CozyKitKeychainDelete(const char* key)
{
    SecItemDelete((__bridge CFDictionaryRef)CozyKitKeychainQuery(key, CozyKitService()));
    SecItemDelete((__bridge CFDictionaryRef)CozyKitKeychainQuery(key, LegacyService));
}

extern "C" void CozyKitKeychainSet(const char* key, const char* value)
{
    CozyKitKeychainDelete(key);
    NSMutableDictionary* item = CozyKitKeychainQuery(key, CozyKitService());
    item[(__bridge id)kSecValueData] = [[NSString stringWithUTF8String:value] dataUsingEncoding:NSUTF8StringEncoding];
    item[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;
    SecItemAdd((__bridge CFDictionaryRef)item, NULL);
}
