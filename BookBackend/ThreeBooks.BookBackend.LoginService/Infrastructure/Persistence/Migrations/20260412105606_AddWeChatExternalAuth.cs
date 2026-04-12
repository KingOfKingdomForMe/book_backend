using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreeBooks.BookBackend.LoginService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeChatExternalAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_auth_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nonce = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ResolvedUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AppId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalOpenId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalUnionId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalNickname = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalAvatarUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FailureCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FailureMessage = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedirectUri = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompletionTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    AuthorizedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: true),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_auth_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_external_auth_sessions_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_external_auth_sessions_users_ResolvedUserId",
                        column: x => x.ResolvedUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "external_identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    AppId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpenId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnionId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nickname = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawProfileJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_identities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_external_identities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_sessions_Provider_Status_ExpiresAtUtc",
                table: "external_auth_sessions",
                columns: new[] { "Provider", "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_sessions_RequestedByUserId",
                table: "external_auth_sessions",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_sessions_ResolvedUserId",
                table: "external_auth_sessions",
                column: "ResolvedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_external_auth_sessions_State",
                table: "external_auth_sessions",
                column: "State",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_identities_Provider_AppId_OpenId",
                table: "external_identities",
                columns: new[] { "Provider", "AppId", "OpenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_identities_Provider_UnionId",
                table: "external_identities",
                columns: new[] { "Provider", "UnionId" });

            migrationBuilder.CreateIndex(
                name: "IX_external_identities_UserId",
                table: "external_identities",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_auth_sessions");

            migrationBuilder.DropTable(
                name: "external_identities");
        }
    }
}
