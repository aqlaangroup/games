using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminRoles",
                columns: table => new
                {
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRoles", x => new { x.AdminUserId, x.GameId });
                });

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Disabled = table.Column<bool>(type: "boolean", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Json = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Template = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    NameAr = table.Column<string>(type: "text", nullable: false),
                    NameEn = table.Column<string>(type: "text", nullable: false),
                    AppleBundleId = table.Column<string>(type: "text", nullable: true),
                    GooglePackageName = table.Column<string>(type: "text", nullable: true),
                    GoogleSignInClientIds = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    CreatedDevice = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletionRequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeleteBy = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OncePerPlayer = table.Column<bool>(type: "boolean", nullable: false),
                    Grants = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Store = table.Column<int>(type: "integer", nullable: false),
                    StoreTransactionId = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaveFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    FromVersion = table.Column<long>(type: "bigint", nullable: false),
                    ToVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaveFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Saves",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Json = table.Column<string>(type: "jsonb", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    Chapter = table.Column<int>(type: "integer", nullable: true),
                    Coins = table.Column<long>(type: "bigint", nullable: true),
                    Device = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Saves", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    Device = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    GameVersion = table.Column<string>(type: "text", nullable: true),
                    Device = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CloseNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletBalances",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletBalances", x => new { x.PlayerId, x.Currency });
                    table.CheckConstraint("CK_WalletBalances_NotNegative", "\"Amount\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "WalletEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Audit_GameId_CreatedAt",
                table: "Audit",
                columns: new[] { "GameId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Configs_GameId_Version",
                table: "Configs",
                columns: new[] { "GameId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_GameId",
                table: "Events",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Slug",
                table: "Games",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Identities_GameId_Provider_Subject",
                table: "Identities",
                columns: new[] { "GameId", "Provider", "Subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Identities_PlayerId_Provider",
                table: "Identities",
                columns: new[] { "PlayerId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_DeleteBy",
                table: "Players",
                column: "DeleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameId",
                table: "Players",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_GameId_Sku",
                table: "Products",
                columns: new[] { "GameId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_GameId_PlayerId",
                table: "Purchases",
                columns: new[] { "GameId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Store_StoreTransactionId",
                table: "Purchases",
                columns: new[] { "Store", "StoreTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaveFlags_GameId_PlayerId",
                table: "SaveFlags",
                columns: new[] { "GameId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Saves_GameId",
                table: "Saves",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FamilyId",
                table: "Sessions",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PlayerId",
                table: "Sessions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TokenHash",
                table: "Sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_GameId_Status",
                table: "SupportTickets",
                columns: new[] { "GameId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WalletBalances_GameId",
                table: "WalletBalances",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletEntries_GameId_PlayerId",
                table: "WalletEntries",
                columns: new[] { "GameId", "PlayerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminRoles");

            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "Audit");

            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Identities");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "SaveFlags");

            migrationBuilder.DropTable(
                name: "Saves");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "WalletBalances");

            migrationBuilder.DropTable(
                name: "WalletEntries");
        }
    }
}
