using System;
using System.Threading.Tasks;
using Aqlaan.CozyKit.Platform;
using Gahwit.Merge;
using Gahwit.UI;
using Newtonsoft.Json.Linq;

namespace Gahwit.Boot
{
    // Friends, gifts, visits, hosting and the Sitti wall through the platform (missing/20). Everything social needs a
    // connection and a player of 13 or older, or a parent's consent (COMPLETE_SPEC.md section 14).
    public sealed class SocialService : ISocialUi
    {
        readonly PlatformClient client;
        readonly Game game;
        readonly Func<bool> allowed;
        readonly Action<string> visit;
        readonly Action postcard;

        public SocialService(PlatformClient client, Game game, Func<bool> allowed, Action<string> visit, Action postcard)
        {
            this.client = client;
            this.game = game;
            this.allowed = allowed;
            this.visit = visit;
            this.postcard = postcard;
        }

        public bool Allowed => allowed();

        static SocialReply From(Reply<JObject> reply) => new SocialReply
        {
            Result = reply.Ok ? ActionResult.Done : reply.Outcome == Outcome.Offline ? ActionResult.Offline : ActionResult.Failed,
            Error = reply.Error,
            Data = reply.Value
        };

        async Task<SocialReply> Call(Func<Task<Reply<JObject>>> call)
        {
            if (!Allowed) return new SocialReply { Result = ActionResult.Unavailable, Error = "parent_needed" };
            return From(await call());
        }

        public Task<SocialReply> Summary() => Call(client.Social);
        public Task<SocialReply> AddFriend(string code) => Call(() => client.AddFriend(code));
        public Task<SocialReply> Gift(string friendId) => Call(() => client.SendGift(friendId));

        public async Task<SocialReply> ClaimGifts()
        {
            var reply = await Call(client.ClaimGifts);
            int energy = reply.Data?.Value<int?>("energy") ?? 0;
            if (reply.Ok && energy > 0) game.AddEnergy(energy);
            return reply;
        }

        public Task<SocialReply> Hosting() => Call(client.Hosting);
        public Task<SocialReply> StartHosting() => Call(client.StartHosting);
        public Task<SocialReply> Invite(string orderId, string friendId) => Call(() => client.InviteToHosting(orderId, friendId));

        // The platform counts the piece first; only then does it leave the board.
        public async Task<SocialReply> Give(string orderId)
        {
            int cell = game.SparePiece();
            if (cell < 0) return new SocialReply { Result = ActionResult.Failed, Error = "nothing_to_give" };
            var reply = await Call(() => client.GiveToHosting(orderId, 1));
            if (reply.Ok) game.GivePiece(cell);
            return reply;
        }

        public Task<SocialReply> Message(string orderId, int preset) => Call(() => client.HostingMessage(orderId, preset));
        public Task<SocialReply> Wall() => Call(client.Wall);
        public Task<SocialReply> Post(string text) => Call(() => client.PostToWall(text));
        public Task<SocialReply> WallAction(string postId, string action) => Call(() => client.WallAction(postId, action));
        public void Visit(string friendId) => visit(friendId);
        public void Postcard() => postcard();
    }
}
