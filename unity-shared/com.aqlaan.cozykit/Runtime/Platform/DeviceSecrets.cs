#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#else
using UnityEngine;
#endif

namespace Aqlaan.CozyKit.Platform
{
    public interface ISecretStore
    {
        string Get(string key);
        void Set(string key, string value);
        void Delete(string key);
    }

    // Where the sign-in token is kept. On iPhone it's the Keychain, which usually outlives a reinstall, so a guest
    // who reinstalls finds her café again. Elsewhere it's PlayerPrefs until Android gets its keystore plugin.
    public sealed class DeviceSecrets : ISecretStore
    {
#if UNITY_IOS && !UNITY_EDITOR
        public string Get(string key) => CozyKitKeychainGet(key);
        public void Set(string key, string value) => CozyKitKeychainSet(key, value);
        public void Delete(string key) => CozyKitKeychainDelete(key);

        [DllImport("__Internal")] static extern string CozyKitKeychainGet(string key);
        [DllImport("__Internal")] static extern void CozyKitKeychainSet(string key, string value);
        [DllImport("__Internal")] static extern void CozyKitKeychainDelete(string key);
#else
        public string Get(string key)
        {
            string value = PlayerPrefs.GetString(key, "");
            return value.Length == 0 ? null : value;
        }

        public void Set(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
#endif
    }
}
