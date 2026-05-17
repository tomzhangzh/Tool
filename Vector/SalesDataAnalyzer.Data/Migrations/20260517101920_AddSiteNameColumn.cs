using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesDataAnalyzer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "VectorData",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "SalesSummaries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "SalesCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiteName",
                table: "HourlySales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "VectorData");

            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "SalesSummaries");

            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "SalesCategories");

            migrationBuilder.DropColumn(
                name: "SiteName",
                table: "HourlySales");
        }
    }
}
