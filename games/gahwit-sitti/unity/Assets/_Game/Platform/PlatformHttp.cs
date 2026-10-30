using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Gahwit.Platform
{
    public struct HttpReply
    {
        public int Status;    // 0 when the platform couldn't be reached
        public string Body;
    }

    public interface IPlatformHttp
    {
        Task<HttpReply> Send(string method, string url, string jsonBody, string bearer);
    }

    // UnityWebRequest on the main thread; the task completes on the main thread too.
    public sealed class UnityPlatformHttp : IPlatformHttp
    {
        const int TimeoutSeconds = 15;

        public Task<HttpReply> Send(string method, string url, string jsonBody, string bearer)
        {
            var request = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = TimeoutSeconds };
            if (jsonBody != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (bearer != null) request.SetRequestHeader("Authorization", "Bearer " + bearer);

            var done = new TaskCompletionSource<HttpReply>();
            request.SendWebRequest().completed += _ =>
            {
                bool reached = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ProtocolError;
                done.SetResult(new HttpReply { Status = reached ? (int)request.responseCode : 0, Body = request.downloadHandler.text });
                request.Dispose();
            };
            return done.Task;
        }
    }
}
