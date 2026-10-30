// Keychain storage for Gahwit.Platform.DeviceSecrets. Items stay on this phone (no iCloud sync) and usually outlive a reinstall.
#import <Foundation/Foundation.h>
#import <Security/Security.h>

static NSMutableDictionary* GahwitKeychainQuery(const char* key)
{
    return [@{
        (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
        (__bridge id)kSecAttrService: @"com.aqlaan.gahwit.platform",
        (__bridge id)kSecAttrAccount: [NSString stringWithUTF8String:key],
    } mutableCopy];
}

extern "C" char* GahwitKeychainGet(const char* key)
{
    NSMutableDictionary* query = GahwitKeychainQuery(key);
    query[(__bridge id)kSecReturnData] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef result = NULL;
    if (SecItemCopyMatching((__bridge CFDictionaryRef)query, &result) != errSecSuccess || result == NULL) return NULL;
    NSData* data = (__bridge_transfer NSData*)result;
    NSString* value = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    return value != nil ? strdup(value.UTF8String) : NULL; // the managed side frees it
}

extern "C" void GahwitKeychainDelete(const char* key)
{
    SecItemDelete((__bridge CFDictionaryRef)GahwitKeychainQuery(key));
}

extern "C" void GahwitKeychainSet(const char* key, const char* value)
{
    GahwitKeychainDelete(key);
    NSMutableDictionary* item = GahwitKeychainQuery(key);
    item[(__bridge id)kSecValueData] = [[NSString stringWithUTF8String:value] dataUsingEncoding:NSUTF8StringEncoding];
    item[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;
    SecItemAdd((__bridge CFDictionaryRef)item, NULL);
}
