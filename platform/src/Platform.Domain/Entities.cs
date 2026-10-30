namespace Platform.Domain;

public enum Store { Apple, Google }
public enum ProductType { Consumable, NonConsumable, Subscription, NonRenewing }
public enum PurchaseStatus { Verified, Refunded, Revoked }
public enum TicketStatus { Open, Closed }

/// <summary>Staff roles, each one including the ones below it.</summary>
public enum StaffRole { Viewer = 1, Support = 2, Producer = 3, Owner = 4 }

/// <summary>A game registered on the platform from its game.json. The one table without a GameId: it is the game.</summary>
public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Slug { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? AppleBundleId { get; set; }
    public string? GooglePackageName { get; set; }
    /// <summary>OAuth client ids a Google identity token may be issued for.</summary>
    public List<string> GoogleSignInClientIds { get; set; } = new();
    /// <summary>From game.json minGameVersion. Older game versions show "update required".</summary>
    public string? MinGameVersion { get; set; }
}

/// <summary>A reward the game earned, such as a level-up or a finished chapter. Each id grants once per player.</summary>
public class RewardClaim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public required string RewardId { get; set; }   // "levelUp:7", "chapter:2"
    public long Gems { get; set; }
    public DateTimeOffset ClaimedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Gems spent in the game. The phone's request id makes a repeated call spend only once.</summary>
public class GemSpend
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public required string RequestId { get; set; }
    public required string Item { get; set; }        // "energyRefill", "storageSlot:1", "bubble:5", "memoryRetry:m2_rhythm"
    public long Gems { get; set; }
    public required string GrantJson { get; set; }  // what the game applies, such as {"energy":100}
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>One account per game. Starts as a guest; Apple or Google identities can be linked later.</summary>
public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public int? BirthYear { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? CreatedDevice { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Set when the player deletes the account. The purge job removes the data by <see cref="DeleteBy"/>.</summary>
    public DateTimeOffset? DeletionRequestedAt { get; set; }
    public DateTimeOffset? DeleteBy { get; set; }
}

/// <summary>An Apple or Google sign-in linked to a player. One per provider per player.</summary>
public class PlayerIdentity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public required string Provider { get; set; }   // "apple" or "google"
    public required string Subject { get; set; }    // the provider's stable user id
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>A refresh token. Only its SHA-256 hash is stored. Each use replaces it with a new one in the same family.</summary>
public class PlayerSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid FamilyId { get; set; }
    public required string TokenHash { get; set; }
    public string? Device { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    /// <summary>Set when the token was swapped for a new one. Using it again means it leaked.</summary>
    public DateTimeOffset? ReplacedAt { get; set; }
}

/// <summary>The player's latest save. Version goes up by one on every upload.</summary>
public class PlayerSave
{
    public Guid PlayerId { get; set; }
    public Guid GameId { get; set; }
    public long Version { get; set; }
    public required string Json { get; set; }
    public int? Level { get; set; }
    public int? Chapter { get; set; }
    public long? Coins { get; set; }
    public string? Device { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>An upload that looked impossible. Flagged for a person to look at, never punished automatically.</summary>
public class SaveFlag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public required string Reason { get; set; }   // level_jump, chapter_jump, coins_jump
    public required string Details { get; set; }
    public long FromVersion { get; set; }
    public long ToVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>One paid currency of one player. Changed only by the server.</summary>
public class WalletBalance
{
    public Guid PlayerId { get; set; }
    public Guid GameId { get; set; }
    public required string Currency { get; set; }
    public long Amount { get; set; }
}

/// <summary>Every change to a wallet, with where it came from.</summary>
public class WalletEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public required string Currency { get; set; }
    public long Amount { get; set; }
    public required string Source { get; set; }   // "purchase", "admin", "reward" or "spend"
    public string? Reason { get; set; }
    public Guid? PurchaseId { get; set; }
    public Guid? AdminUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string Sku { get; set; }
    public ProductType Type { get; set; }
    public bool OncePerPlayer { get; set; }
    /// <summary>What the product grants, e.g. {"gems":"100"}. Numbers are currencies; other values are item ids.</summary>
    public Dictionary<string, string> Grants { get; set; } = new();
}

public class Purchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    /// <summary>Cleared when the player's account is purged. The purchase stays for accounting.</summary>
    public Guid? PlayerId { get; set; }
    public Store Store { get; set; }
    /// <summary>Unique per store. Stops the same receipt granting twice.</summary>
    public required string StoreTransactionId { get; set; }
    public required string Sku { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Verified;
    public DateTimeOffset PurchasedAt { get; set; }
    public DateTimeOffset VerifiedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>A message from the in-game Help form.</summary>
public class SupportTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public required string Message { get; set; }
    public string? Email { get; set; }
    public string? GameVersion { get; set; }
    public string? Device { get; set; }
    public string? Language { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public string? CloseNote { get; set; }
}

/// <summary>A published copy of a game's balance.json.</summary>
public class ConfigVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public int Version { get; set; }
    public required string Json { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class LiveEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string Template { get; set; }   // race, album, pass, miniBoard, frenzy, hospitality
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public required string SettingsJson { get; set; }
}

/// <summary>A staff account. Staff work across games, so this table has no GameId; access is per game in <see cref="AdminRole"/>.</summary>
public class AdminUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool Disabled { get; set; }
    /// <summary>True after a temporary password; staff must set their own before doing anything else.</summary>
    public bool MustChangePassword { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>A staff member's role on one game.</summary>
public class AdminRole
{
    public Guid AdminUserId { get; set; }
    public Guid GameId { get; set; }
    public StaffRole Role { get; set; }
}

/// <summary>Every write a staff member makes on a game.</summary>
public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid AdminUserId { get; set; }
    public required string Action { get; set; }
    public required string Target { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
