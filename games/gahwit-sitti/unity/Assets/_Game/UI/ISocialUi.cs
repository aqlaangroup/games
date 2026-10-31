using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Gahwit.UI
{
    public sealed class SocialReply
    {
        public ActionResult Result;
        public string Error;  // the platform's error, such as "unknown_code" or "daily_limit"
        public JObject Data;
        public bool Ok => Result == ActionResult.Done;
    }

    // What the friends, hosting and Sitti wall screens need (Boot.SocialService; missing/20).
    public interface ISocialUi
    {
        bool Allowed { get; } // 13 or older, or a parent agreed
        Task<SocialReply> Summary();
        Task<SocialReply> AddFriend(string code);
        Task<SocialReply> Gift(string friendId);
        Task<SocialReply> ClaimGifts();               // adds the gifts' energy
        Task<SocialReply> Hosting();
        Task<SocialReply> StartHosting();
        Task<SocialReply> Invite(string orderId, string friendId);
        Task<SocialReply> Give(string orderId);        // the piece the player needs least
        Task<SocialReply> Message(string orderId, int preset);
        Task<SocialReply> Wall();
        Task<SocialReply> Post(string text);
        Task<SocialReply> WallAction(string postId, string action); // like, report, block
        void Visit(string friendId);
        void Postcard(string line = null); // a postcard of the café; line replaces the usual one (a hosting order's)
    }
}
