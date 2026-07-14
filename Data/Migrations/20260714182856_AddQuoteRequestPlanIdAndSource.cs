using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteRequestPlanIdAndSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanId",
                table: "QuoteRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "QuoteRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_PlanId",
                table: "QuoteRequests",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteRequests_InsurancePlans_PlanId",
                table: "QuoteRequests",
                column: "PlanId",
                principalTable: "InsurancePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteRequests_InsurancePlans_PlanId",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_PlanId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "QuoteRequests");
        }
    }
}
