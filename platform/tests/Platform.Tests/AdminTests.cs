using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Api.Admin;
using Platform.Domain;
using static Platform.Tests.PlatformFactory;

namespace Platform.Tests;

[Collection("platform")]
public class AdminTests(PlatformFactory api)
{
    static string Email(string who) => $"{who}.{Guid.NewGuid():N}@aqlaan.test";

    [Fact]
    public async Task ViewersCanLookButOnlySupportAndUpCanGrant()
    {
        var player = Guid.Parse((await api.Guest()).Str("playerId"));
        var (_, viewer) = await api.Staff(Email("viewer"), (GameA, StaffRole.Viewer));
        var (supportId, support) = await api.Staff(Email("support"), (GameA, StaffRole.Support));

        Assert.Equal(HttpStatusCode.OK, (await api.Client(viewer).GetAsync($"/games/{GameA}/admin/players/{player}")).StatusCode);
        var denied = await api.Client(viewer).PostAsJsonAsync($"/games/{GameA}/admin/players/{player}/grants",
            new { currency = "gems", amount = 50, reason = "lost purchase" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var grant = await api.Client(support).PostAsJsonAsync($"/games/{GameA}/admin/players/{player}/grants",
            new { currency = "gems", amount = 50, reason = "lost purchase" });
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);
        Assert.Equal(50, (await grant.Json()).GetProperty("wallet").GetProperty("gems").GetInt64());

        await api.WithDb(async db =>
        {
            var entry = await db.WalletEntries.SingleAsync(e => e.PlayerId == player);
            Assert.Equal(("admin", 50L, "lost purchase", supportId), (entry.Source, entry.Amount, entry.Reason, entry.AdminUserId));
            var audit = await db.Audit.SingleAsync(a => a.Target == $"player:{player}");
            Assert.Equal(("wallet.grant", supportId), (audit.Action, audit.AdminUserId));
            return 0;
        });

        var detail = await (await api.Client(viewer).GetAsync($"/games/{GameA}/admin/players/{player}")).Json();
        Assert.Equal(50, detail.GetProperty("wallet").GetProperty("gems").GetInt64());
        Assert.Equal("lost purchase", detail.GetProperty("ledger")[0].Str("reason"));
    }

    [Fact]
    public async Task StaffOnOneGameHaveNoAccessToAnother()
    {
        var player = (await api.Guest(GameB)).Str("playerId");
        var (_, owner) = await api.Staff(Email("owner-a"), (GameA, StaffRole.Owner));
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Client(owner).GetAsync($"/games/{GameB}/admin/players/{player}")).StatusCode);
        // A player's token is not a staff token.
        var playerToken = (await api.Guest()).Str("accessToken");
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client(playerToken).GetAsync($"/games/{GameA}/admin/players")).StatusCode);
    }

    [Fact]
    public async Task AGrantCannotTakeAWalletBelowZero()
    {
        var player = Guid.Parse((await api.Guest()).Str("playerId"));
        var (_, support) = await api.Staff(Email("support"), (GameA, StaffRole.Support));
        var client = api.Client(support);
        await client.PostAsJsonAsync($"/games/{GameA}/admin/players/{player}/grants", new { currency = "gems", amount = 10, reason = "gift" });
        var overdraw = await client.PostAsJsonAsync($"/games/{GameA}/admin/players/{player}/grants", new { currency = "gems", amount = -11, reason = "fix" });
        Assert.Equal(HttpStatusCode.BadRequest, overdraw.StatusCode);
        Assert.Equal("insufficient_balance", (await overdraw.Json()).Str("error"));
        var noReason = await client.PostAsJsonAsync($"/games/{GameA}/admin/players/{player}/grants", new { currency = "gems", amount = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var fix = await client.PostAsJsonAsync($"/games/{GameA}/admin/players/{player}/grants", new { currency = "gems", amount = -10, reason = "fix" });
        var body = await fix.Json();
        Assert.True(fix.IsSuccessStatusCode, body.ToString());
        Assert.Equal(0, body.GetProperty("wallet").GetProperty("gems").GetInt64());
    }

    [Fact]
    public async Task OnlyOwnersManageStaffAndNewStaffMustSetTheirOwnPassword()
    {
        var (_, owner) = await api.Staff(Email("owner"), (GameA, StaffRole.Owner));
        var (_, support) = await api.Staff(Email("support"), (GameA, StaffRole.Support));
        var newEmail = Email("new");

        Assert.Equal(HttpStatusCode.Forbidden, (await api.Client(support).GetAsync($"/games/{GameA}/admin/staff")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Client(support).PutAsJsonAsync($"/games/{GameA}/admin/staff", new { email = newEmail, role = "owner" })).StatusCode);

        var added = await (await api.Client(owner).PutAsJsonAsync($"/games/{GameA}/admin/staff", new { email = newEmail, role = "support" })).Json();
        var temporary = added.Str("temporaryPassword");
        Assert.Equal("support", added.Str("role"));

        var signIn = await (await api.Client().PostAsJsonAsync("/admin/sign-in", new { email = newEmail, password = temporary })).Json();
        Assert.True(signIn.GetProperty("admin").GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal("support", signIn.GetProperty("admin").GetProperty("roles").Str(GameA));
        var newcomer = api.Client(signIn.Str("accessToken"));
        var blocked = await newcomer.GetAsync($"/games/{GameA}/admin/support");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("password_change_required", (await blocked.Json()).Str("error"));

        Assert.Equal(HttpStatusCode.BadRequest, (await newcomer.PostAsJsonAsync("/admin/me/password", new { currentPassword = temporary, newPassword = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await newcomer.PostAsJsonAsync("/admin/me/password", new { currentPassword = temporary, newPassword = "a-long-new-password" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await newcomer.GetAsync($"/games/{GameA}/admin/support")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client().PostAsJsonAsync("/admin/sign-in", new { email = newEmail, password = temporary })).StatusCode);

        var staff = await (await api.Client(owner).GetAsync($"/games/{GameA}/admin/staff")).Json();
        Assert.Contains(staff.EnumerateArray(), s => s.Str("email") == newEmail);
        var audit = await (await api.Client(owner).GetAsync($"/games/{GameA}/admin/audit")).Json();
        Assert.Contains(audit.EnumerateArray(), a => a.Str("action") == "staff.set" && a.Str("details").Contains(newEmail));
    }

    [Fact]
    public async Task TheLastOwnerOfAGameCannotBeRemoved()
    {
        // Test B gets exactly one owner here (other tests share the database).
        var (ownerId, owner) = await api.Staff(Email("owner-b"), (GameB, StaffRole.Owner));
        await api.WithDb(db => db.AdminRoles
            .Where(r => r.Role == StaffRole.Owner && r.AdminUserId != ownerId && r.GameId == db.Games.Single(g => g.Slug == GameB).Id)
            .ExecuteDeleteAsync());
        var remove = await api.Client(owner).DeleteAsync($"/games/{GameB}/admin/staff/{ownerId}");
        Assert.Equal(HttpStatusCode.BadRequest, remove.StatusCode);
        Assert.Equal("last_owner", (await remove.Json()).Str("error"));
    }

    [Fact]
    public async Task CreateOwnerMakesAnOwnerOfEveryGameWithATemporaryPassword()
    {
        var email = Email("first");
        var password = await api.With<StaffAccounts, string>(s => s.CreateOwnerAsync(email, default));
        var signIn = await (await api.Client().PostAsJsonAsync("/admin/sign-in", new { email, password })).Json();
        var roles = signIn.GetProperty("admin").GetProperty("roles");
        Assert.Equal("owner", roles.Str(GameA));
        Assert.Equal("owner", roles.Str(GameB));
        Assert.True(signIn.GetProperty("admin").GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task DisabledStaffAreSignedOutAtOnce()
    {
        var (id, token) = await api.Staff(Email("leaver"), (GameA, StaffRole.Viewer));
        await api.WithDb(db => db.AdminUsers.Where(a => a.Id == id).ExecuteUpdateAsync(u => u.SetProperty(a => a.Disabled, true)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client(token).GetAsync($"/games/{GameA}/admin/players")).StatusCode);
    }

    [Fact]
    public async Task SupportFindsAPlayerByTheirAppleIdAndSeesItMasked()
    {
        var subject = $"001122.{Guid.NewGuid():N}.3344";
        var player = await (await api.Client().PostAsJsonAsync($"/games/{GameA}/players/sign-in",
            new { provider = "apple", idToken = api.Jwks.Apple(subject) })).Json();
        var (_, viewer) = await api.Staff(Email("viewer"), (GameA, StaffRole.Viewer));

        var found = await (await api.Client(viewer).GetAsync($"/games/{GameA}/admin/players?query={subject}")).Json();
        Assert.Equal(player.Str("playerId"), found[0].Str("playerId"));
        var byId = await (await api.Client(viewer).GetAsync($"/games/{GameA}/admin/players?query={player.Str("playerId")}")).Json();
        Assert.Equal(1, byId.GetArrayLength());

        var detail = await (await api.Client(viewer).GetAsync($"/games/{GameA}/admin/players/{player.Str("playerId")}")).Json();
        var shown = detail.GetProperty("identities")[0].Str("subject");
        Assert.DoesNotContain(subject, shown);
        Assert.StartsWith("0011", shown);
    }

    [Fact]
    public async Task APlayersHelpMessageReachesSupportWhoCloseIt()
    {
        var guest = await api.Guest();
        var sent = await api.Client(guest.Str("accessToken")).PostAsJsonAsync($"/games/{GameA}/players/me/support",
            new { message = "My gems are missing", email = "player@example.com", gameVersion = "0.1.0", device = "iPhone15,2", language = "ar" });
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var ticket = (await sent.Json()).Str("ticketId");
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client(guest.Str("accessToken"))
            .PostAsJsonAsync($"/games/{GameA}/players/me/support", new { message = " " })).StatusCode);

        var (_, support) = await api.Staff(Email("support"), (GameA, StaffRole.Support));
        var open = await (await api.Client(support).GetAsync($"/games/{GameA}/admin/support")).Json();
        var row = open.EnumerateArray().Single(t => t.Str("id") == ticket);
        Assert.Equal(guest.Str("playerId"), row.Str("playerId"));
        Assert.Equal("iPhone15,2", row.Str("device"));

        var close = await api.Client(support).PostAsJsonAsync($"/games/{GameA}/admin/support/{ticket}/close", new { note = "Granted 100 gems" });
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        var stillOpen = await (await api.Client(support).GetAsync($"/games/{GameA}/admin/support?status=open")).Json();
        Assert.DoesNotContain(stillOpen.EnumerateArray(), t => t.Str("id") == ticket);
    }
}
