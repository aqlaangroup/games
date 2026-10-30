namespace Platform.Domain;

public enum Store { Apple, Google }
public enum ProductType { Consumable, NonConsumable, Subscription }
public enum PurchaseStatus { Verified, Refunded, Revoked }

/// <summary>A game registered on the platform from its game.json.</summary>
public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Slug { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? AppleBundleId { get; set; }
    public string? GooglePackageName { get; set; }
}

/// <summary>One account per game. Every game-owned record carries GameId.</summary>
public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string SignInProvider { get; set; }   // "apple" or "google"
    public required string SignInSubject { get; set; }    // the provider's stable user id
    public string? Country { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PlayerSave
{
    public Guid PlayerId { get; set; }
    public Guid GameId { get; set; }
    public long Version { get; set; }
    public required string Json { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Paid currencies. Changed only by the server.</summary>
public class Wallet
{
    public Guid PlayerId { get; set; }
    public Guid GameId { get; set; }
    public Dictionary<string, long> Balances { get; set; } = new();
}

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string Sku { get; set; }
    public ProductType Type { get; set; }
    public bool OncePerPlayer { get; set; }
    /// <summary>What the product grants, e.g. {"gems":100}. Non-currency grants are stored as item ids.</summary>
    public Dictionary<string, string> Grants { get; set; } = new();
}

public class Purchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
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

public class AdminUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    /// <summary>Role per game: owner, producer, support, viewer.</summary>
    public Dictionary<Guid, string> GameRoles { get; set; } = new();
}
