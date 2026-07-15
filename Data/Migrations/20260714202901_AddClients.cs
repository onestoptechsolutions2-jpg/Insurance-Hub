using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Create Clients first — nothing can be backfilled into it otherwise ──────────
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_UserId",
                table: "Clients",
                column: "UserId");

            // ── 2. Add ClientId as NULLABLE for now — backfilled below, then locked to NOT NULL ─
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "UserPolicies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRate",
                table: "UserPolicies",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "QuoteRequests",
                type: "integer",
                nullable: true);

            // ── 3. Backfill: one Client per distinct AspNetUsers row already referenced by a
            //    QuoteRequest or UserPolicy, sourced from that user's own profile fields ────────
            migrationBuilder.Sql(@"
                INSERT INTO ""Clients"" (""FullName"", ""Email"", ""Phone"", ""UserId"", ""CreatedAt"")
                SELECT DISTINCT
                    COALESCE(NULLIF(u.""DisplayName"", ''), u.""Email""),
                    u.""Email"",
                    COALESCE(u.""PhoneNumber"", ''),
                    u.""Id"",
                    now()
                FROM ""AspNetUsers"" u
                WHERE u.""Id"" IN (
                    SELECT ""UserId"" FROM ""QuoteRequests"" WHERE ""UserId"" IS NOT NULL
                    UNION
                    SELECT ""UserId"" FROM ""UserPolicies"" WHERE ""UserId"" IS NOT NULL
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE ""QuoteRequests"" q
                SET ""ClientId"" = c.""Id""
                FROM ""Clients"" c
                WHERE c.""UserId"" = q.""UserId"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""UserPolicies"" p
                SET ""ClientId"" = c.""Id""
                FROM ""Clients"" c
                WHERE c.""UserId"" = p.""UserId"";
            ");

            // ── 4. Defensive fallback: any QuoteRequest still missing a ClientId (shouldn't exist
            //    yet, but the inbound Leads API added last release makes it possible going
            //    forward) gets a Client synthesized from its own snapshot fields ──────────────────
            migrationBuilder.Sql(@"
                INSERT INTO ""Clients"" (""FullName"", ""Email"", ""Phone"", ""CreatedAt"")
                SELECT DISTINCT q.""FullName"", q.""Email"", '', now()
                FROM ""QuoteRequests"" q
                WHERE q.""ClientId"" IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""QuoteRequests"" q
                SET ""ClientId"" = c.""Id""
                FROM ""Clients"" c
                WHERE c.""Email"" = q.""Email"" AND q.""ClientId"" IS NULL;
            ");

            // ── 5. Every row now has a ClientId — lock the columns down ─────────────────────────
            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "QuoteRequests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "UserPolicies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // ── 6. Indexes + FKs on the new columns ─────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ClientId",
                table: "QuoteRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicies_ClientId",
                table: "UserPolicies",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteRequests_Clients_ClientId",
                table: "QuoteRequests",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPolicies_Clients_ClientId",
                table: "UserPolicies",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ── 7. Only now is it safe to drop the old UserId columns/FK/index ──────────────────
            migrationBuilder.DropForeignKey(
                name: "FK_UserPolicies_AspNetUsers_UserId",
                table: "UserPolicies");

            migrationBuilder.DropIndex(
                name: "IX_UserPolicies_UserId",
                table: "UserPolicies");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserPolicies");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "QuoteRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteRequests_Clients_ClientId",
                table: "QuoteRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPolicies_Clients_ClientId",
                table: "UserPolicies");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_UserPolicies_ClientId",
                table: "UserPolicies");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_ClientId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "UserPolicies");

            migrationBuilder.DropColumn(
                name: "CommissionRate",
                table: "UserPolicies");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "QuoteRequests");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserPolicies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "QuoteRequests",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicies_UserId",
                table: "UserPolicies",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPolicies_AspNetUsers_UserId",
                table: "UserPolicies",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
