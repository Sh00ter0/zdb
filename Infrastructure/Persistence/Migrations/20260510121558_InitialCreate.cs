using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationClients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", nullable: false),
                    KeyPreview = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationClients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    HierarchyWeight = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZabbixCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssociatedIntegrationClientId = table.Column<long>(type: "INTEGER", nullable: false),
                    ApiUrl = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedApiToken = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZabbixCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZabbixCredentials_IntegrationClients_AssociatedIntegrationClientId",
                        column: x => x.AssociatedIntegrationClientId,
                        principalTable: "IntegrationClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_SystemRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SystemRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemAdministrators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<long>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemManaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAdministrators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAdministrators_SystemAdministrators_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "SystemAdministrators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SystemAdministrators_SystemRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "SystemRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnownDeliveryTargets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntegrationClientId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<long>(type: "INTEGER", nullable: false),
                    TargetId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelType = table.Column<int>(type: "INTEGER", nullable: false),
                    AssociatedGuildId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    AutoCrosspost = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownDeliveryTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnownDeliveryTargets_IntegrationClients_IntegrationClientId",
                        column: x => x.IntegrationClientId,
                        principalTable: "IntegrationClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnownDeliveryTargets_SystemAdministrators_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "SystemAdministrators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Key" },
                values: new object[,]
                {
                    { 1, "Root permission", "root" },
                    { 100, "Allow to read system administrators", "system.admins.read" },
                    { 101, "Allow to create and modify system administrators", "system.admins.write" },
                    { 200, "Allow to read API clients", "api.clients.read" },
                    { 201, "Allow to create and modify API clients", "api.clients.write" },
                    { 300, "Allow to read known delivery targets", "api.knownTargets.read" },
                    { 301, "Allow to create and modify known delivery targets", "api.knownTargets.write" }
                });

            migrationBuilder.InsertData(
                table: "SystemRoles",
                columns: new[] { "Id", "HierarchyWeight", "Name" },
                values: new object[,]
                {
                    { 1, 1000, "Super Administrator" },
                    { 2, 500, "Administrator" },
                    { 3, 100, "Moderator" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 100, 2 },
                    { 101, 2 },
                    { 200, 2 },
                    { 201, 2 },
                    { 300, 2 },
                    { 301, 2 },
                    { 100, 3 },
                    { 200, 3 },
                    { 300, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationClients_KeyHash",
                table: "IntegrationClients",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationClients_Name",
                table: "IntegrationClients",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnownDeliveryTargets_AssociatedGuildId",
                table: "KnownDeliveryTargets",
                column: "AssociatedGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_KnownDeliveryTargets_CreatedById",
                table: "KnownDeliveryTargets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_KnownDeliveryTargets_IntegrationClientId_Name",
                table: "KnownDeliveryTargets",
                columns: new[] { "IntegrationClientId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnownDeliveryTargets_IntegrationClientId_TargetId",
                table: "KnownDeliveryTargets",
                columns: new[] { "IntegrationClientId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdministrators_CreatedById",
                table: "SystemAdministrators",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdministrators_DiscordUserId",
                table: "SystemAdministrators",
                column: "DiscordUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdministrators_RoleId",
                table: "SystemAdministrators",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ZabbixCredentials_AssociatedIntegrationClientId",
                table: "ZabbixCredentials",
                column: "AssociatedIntegrationClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnownDeliveryTargets");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "ZabbixCredentials");

            migrationBuilder.DropTable(
                name: "SystemAdministrators");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "IntegrationClients");

            migrationBuilder.DropTable(
                name: "SystemRoles");
        }
    }
}
