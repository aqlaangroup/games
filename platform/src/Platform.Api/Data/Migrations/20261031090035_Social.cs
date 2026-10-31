using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Social : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FriendCode",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParentConsent",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<string>(type: "text", nullable: false),
                    Energy = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HostingHelpers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Given = table.Column<int>(type: "integer", nullable: false),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostingHelpers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HostingOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ItemsNeeded = table.Column<int>(type: "integer", nullable: false),
                    ItemsGiven = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MessagesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostingOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WallPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AutoReason = table.Column<string>(type: "text", nullable: true),
                    Likes = table.Column<int>(type: "integer", nullable: false),
                    Reports = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WallPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WallReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WallReactions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameId_FriendCode",
                table: "Players",
                columns: new[] { "GameId", "FriendCode" },
                unique: true,
                filter: "\"FriendCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_PlayerId_BlockedId",
                table: "Blocks",
                columns: new[] { "PlayerId", "BlockedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_PlayerId_FriendId",
                table: "Friendships",
                columns: new[] { "PlayerId", "FriendId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gifts_FromPlayerId_ToPlayerId_Day",
                table: "Gifts",
                columns: new[] { "FromPlayerId", "ToPlayerId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gifts_ToPlayerId",
                table: "Gifts",
                column: "ToPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_HostingHelpers_OrderId_PlayerId",
                table: "HostingHelpers",
                columns: new[] { "OrderId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HostingOrders_HostId_Status",
                table: "HostingOrders",
                columns: new[] { "HostId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WallPosts_GameId_Status",
                table: "WallPosts",
                columns: new[] { "GameId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WallReactions_PlayerId_PostId_Kind",
                table: "WallReactions",
                columns: new[] { "PlayerId", "PostId", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "Gifts");

            migrationBuilder.DropTable(
                name: "HostingHelpers");

            migrationBuilder.DropTable(
                name: "HostingOrders");

            migrationBuilder.DropTable(
                name: "WallPosts");

            migrationBuilder.DropTable(
                name: "WallReactions");

            migrationBuilder.DropIndex(
                name: "IX_Players_GameId_FriendCode",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "FriendCode",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "ParentConsent",
                table: "Players");
        }
    }
}
