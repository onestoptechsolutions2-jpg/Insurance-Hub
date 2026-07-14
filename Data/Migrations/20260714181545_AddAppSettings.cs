using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AgentContactEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    SmtpUseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpSenderEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    SmtpSenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SmtpSenderPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SmtpAgentEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AfricasTalkingUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AfricasTalkingApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AfricasTalkingSenderId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LeadApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
