using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Insurance_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Providers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Providers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Providers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Providers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CoverageLimit",
                table: "InsurancePlans",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Deductible",
                table: "InsurancePlans",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "InsurancePlans",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "InsurancePlans",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPopular",
                table: "InsurancePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "InsurancePlans",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "InsurancePlans",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "CoverageLimit",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Deductible",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "IsPopular",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "InsurancePlans");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "InsurancePlans");
        }
    }
}
