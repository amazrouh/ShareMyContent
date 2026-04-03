using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareShowcase.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShareLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLinks", x => x.Id);
                    table.CheckConstraint("CK_ShareLink_Target", "(\"MediaAssetId\" IS NOT NULL AND \"FolderId\" IS NULL) OR (\"MediaAssetId\" IS NULL AND \"FolderId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ShareLinks_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareLinks_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareLinks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShareAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareLinkId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AccessType = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareAccessLogs_ShareLinks_ShareLinkId",
                        column: x => x.ShareLinkId,
                        principalTable: "ShareLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShareAccessLogs_ShareLinkId",
                table: "ShareAccessLogs",
                column: "ShareLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_FolderId",
                table: "ShareLinks",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_MediaAssetId",
                table: "ShareLinks",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_OwnerId",
                table: "ShareLinks",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareLinks_Token",
                table: "ShareLinks",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShareAccessLogs");

            migrationBuilder.DropTable(
                name: "ShareLinks");
        }
    }
}
