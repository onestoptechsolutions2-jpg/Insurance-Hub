using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InsuranceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyPremium = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RenewalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemindersEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPolicies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicies_UserId",
                table: "UserPolicies",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPolicies");
        }
    }
}
