using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Platform.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RewardsAndSpending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MinGameVersion",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GemSpends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    Item = table.Column<string>(type: "text", nullable: false),
                    Gems = table.Column<long>(type: "bigint", nullable: false),
                    GrantJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GemSpends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardId = table.Column<string>(type: "text", nullable: false),
                    Gems = table.Column<long>(type: "bigint", nullable: false),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardClaims", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GemSpends_PlayerId_Item",
                table: "GemSpends",
                columns: new[] { "PlayerId", "Item" });

            migrationBuilder.CreateIndex(
                name: "IX_GemSpends_PlayerId_RequestId",
                table: "GemSpends",
                columns: new[] { "PlayerId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RewardClaims_PlayerId_RewardId",
                table: "RewardClaims",
                columns: new[] { "PlayerId", "RewardId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GemSpends");

            migrationBuilder.DropTable(
                name: "RewardClaims");

            migrationBuilder.DropColumn(
                name: "MinGameVersion",
                table: "Games");
        }
    }
}
