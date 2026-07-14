using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteSoldLostAndConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // QuoteStatus.Closed is replaced by Sold/Lost. Status is stored as a plain
            // varchar (HasConversion<string>()), so existing rows need a data fix-up rather
            // than a schema change. Sold is the safe default — the broker can correct any
            // that were actually lost via the admin UI.
            migrationBuilder.Sql(
                "UPDATE \"QuoteRequests\" SET \"Status\" = 'Sold' WHERE \"Status\" = 'Closed';");

            migrationBuilder.AddColumn<int>(
                name: "ConvertedPolicyId",
                table: "QuoteRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ConvertedPolicyId",
                table: "QuoteRequests",
                column: "ConvertedPolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteRequests_UserPolicies_ConvertedPolicyId",
                table: "QuoteRequests",
                column: "ConvertedPolicyId",
                principalTable: "UserPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteRequests_UserPolicies_ConvertedPolicyId",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_ConvertedPolicyId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ConvertedPolicyId",
                table: "QuoteRequests");
        }
    }
}
