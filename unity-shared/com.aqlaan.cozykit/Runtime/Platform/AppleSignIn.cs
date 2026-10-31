using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Aqlaan.CozyKit.Platform
{
    // Sign in with Apple through Plugins/iOS/CozyKitAppleSignIn.mm. Asks for no name and no email: the game
    // only needs Apple's stable id. Apple puts the SHA-256 of the nonce in the token; the platform checks it
    // against the plain nonce, so a stolen token can't be replayed.
    public static class AppleSignIn
    {
        public struct Result
        {
            public string IdToken;
            public string Nonce;
            public bool Cancelled;
        }

#if UNITY_IOS && !UNITY_EDITOR
        public static bool Available => true;

        delegate void Callback(string idToken, string error);

        [DllImport("__Internal")] static extern void CozyKitAppleSignIn(string hashedNonce, Callback callback);

        static TaskCompletionSource<Result> pending;
        static string pendingNonce;

        public static Task<Result> Request()
        {
            pending?.TrySetResult(new Result { Cancelled = true });
            pending = new TaskCompletionSource<Result>();
            pendingNonce = NewNonce();
            CozyKitAppleSignIn(Sha256Hex(pendingNonce), OnResult);
            return pending.Task;
        }

        [AOT.MonoPInvokeCallback(typeof(Callback))]
        static void OnResult(string idToken, string error)
        {
            if (error != null && error != "canceled") UnityEngine.Debug.LogWarning("[AppleSignIn] " + error);
            pending?.TrySetResult(new Result { IdToken = idToken, Nonce = pendingNonce, Cancelled = idToken == null });
            pending = null;
        }
#else
        public static bool Available => false;

        public static Task<Result> Request() => Task.FromResult(new Result { Cancelled = true });
#endif

        public static string NewNonce()
        {
            var bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return Hex(bytes);
        }

        public static string Sha256Hex(string text)
        {
            using (var sha = SHA256.Create()) return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
