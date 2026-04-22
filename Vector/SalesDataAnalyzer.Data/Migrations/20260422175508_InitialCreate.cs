using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesDataAnalyzer.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HourlySales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MerchOnlyCount = table.Column<int>(type: "int", nullable: false),
                    MerchOnlyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MerchFuelCount = table.Column<int>(type: "int", nullable: false),
                    MerchFuelAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FuelOnlyCount = table.Column<int>(type: "int", nullable: false),
                    FuelOnlyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegisterId = table.Column<int>(type: "int", nullable: true),
                    PeriodBeginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlySales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    PaymentSysId = table.Column<int>(type: "int", nullable: false),
                    PaymentName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsCardBased = table.Column<bool>(type: "bit", nullable: false),
                    SaleCount = table.Column<int>(type: "int", nullable: false),
                    SaleAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancelRefundCount = table.Column<int>(type: "int", nullable: false),
                    CancelRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegisterId = table.Column<int>(type: "int", nullable: true),
                    CashierId = table.Column<int>(type: "int", nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PeriodBeginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategorySysId = table.Column<int>(type: "int", nullable: false),
                    NetSalesCount = table.Column<int>(type: "int", nullable: false),
                    NetSalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetSalesItemCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentOfSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PeriodBeginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegisterId = table.Column<int>(type: "int", nullable: true),
                    CashierId = table.Column<int>(type: "int", nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    InsideGrandStart = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsideSalesStart = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutsideGrandStart = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutsideSalesStart = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsideGrandEnd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsideSalesEnd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutsideGrandEnd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutsideSalesEnd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsideGrandDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsideSalesDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutsideGrandDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutsideSalesDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    CustomerCount = table.Column<int>(type: "int", nullable: false),
                    NoSaleCount = table.Column<int>(type: "int", nullable: false),
                    FuelSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MerchSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegisterId = table.Column<int>(type: "int", nullable: true),
                    CashierId = table.Column<int>(type: "int", nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PeriodBeginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VectorData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    PeriodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChromaDocumentId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VectorData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HourlySales_SiteId_Hour_PeriodBeginDate",
                table: "HourlySales",
                columns: new[] { "SiteId", "Hour", "PeriodBeginDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_SiteId_PaymentSysId_PeriodBeginDate",
                table: "PaymentMethods",
                columns: new[] { "SiteId", "PaymentSysId", "PeriodBeginDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesCategories_SiteId_CategorySysId_PeriodBeginDate",
                table: "SalesCategories",
                columns: new[] { "SiteId", "CategorySysId", "PeriodBeginDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesSummaries_SiteId_PeriodBeginDate",
                table: "SalesSummaries",
                columns: new[] { "SiteId", "PeriodBeginDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VectorData_SiteId_PeriodDate_DataType",
                table: "VectorData",
                columns: new[] { "SiteId", "PeriodDate", "DataType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HourlySales");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "SalesCategories");

            migrationBuilder.DropTable(
                name: "SalesSummaries");

            migrationBuilder.DropTable(
                name: "VectorData");
        }
    }
}
