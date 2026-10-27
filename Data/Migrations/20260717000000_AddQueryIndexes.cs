using Insurance_Hub.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260717000000_AddQueryIndexes")]
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_RequestedAt",
                table: "QuoteRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicies_RenewalDate",
                table: "UserPolicies",
                column: "RenewalDate");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PeriodMonth",
                table: "Transactions",
                column: "PeriodMonth");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RecordedAt",
                table: "Transactions",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_QuoteRequests_RequestedAt", table: "QuoteRequests");
            migrationBuilder.DropIndex(name: "IX_UserPolicies_RenewalDate", table: "UserPolicies");
            migrationBuilder.DropIndex(name: "IX_Transactions_PeriodMonth", table: "Transactions");
            migrationBuilder.DropIndex(name: "IX_Transactions_RecordedAt", table: "Transactions");
        }
    }
}
