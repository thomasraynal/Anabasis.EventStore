using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Anabasis.EntityFramework.Tests.Integration.Migrations
{
    public partial class AddDataset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "CurrencyPairs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TickFrequency",
                table: "CurrencyPairs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Counterparties",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counterparties", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    TradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyPairCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CounterpartyCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    BuyOrSell = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.TradeId);
                    table.ForeignKey(
                        name: "FK_Trades_Counterparties_CounterpartyCode",
                        column: x => x.CounterpartyCode,
                        principalTable: "Counterparties",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Trades_CurrencyPairs_CurrencyPairCode",
                        column: x => x.CurrencyPairCode,
                        principalTable: "CurrencyPairs",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Counterparties",
                column: "Name",
                values: new object[]
                {
                    "Bank Of America",
                    "Nomura",
                    "HSBC",
                    "SGCIB"
                });

            migrationBuilder.UpdateData(
                table: "CurrencyPairs",
                keyColumn: "Code",
                keyValue: "EUR/USD",
                columns: new[] { "DecimalPlaces", "TickFrequency" },
                values: new object[] { 4, 3m });

            migrationBuilder.InsertData(
                table: "CurrencyPairs",
                columns: new[] { "Code", "Currency1Code", "Currency2Code", "DecimalPlaces", "TickFrequency" },
                values: new object[,]
                {
                    { "EUR/GBP", "EUR", "GBP", 4, 3m },
                    { "EUR/JPY", "EUR", "JPY", 4, 3m },
                    { "USD/GBP", "USD", "GBP", 4, 3m },
                    { "USD/JPY", "USD", "JPY", 4, 3m },
                    { "GBP/JPY", "GBP", "JPY", 4, 3m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_CounterpartyCode",
                table: "Trades",
                column: "CounterpartyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_CurrencyPairCode",
                table: "Trades",
                column: "CurrencyPairCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "Counterparties");

            migrationBuilder.DeleteData(
                table: "CurrencyPairs",
                keyColumn: "Code",
                keyValue: "EUR/GBP");

            migrationBuilder.DeleteData(
                table: "CurrencyPairs",
                keyColumn: "Code",
                keyValue: "EUR/JPY");

            migrationBuilder.DeleteData(
                table: "CurrencyPairs",
                keyColumn: "Code",
                keyValue: "GBP/JPY");

            migrationBuilder.DeleteData(
                table: "CurrencyPairs",
                keyColumn: "Code",
                keyValue: "USD/GBP");

            migrationBuilder.DeleteData(
                table: "CurrencyPairs",
                keyColumn: "Code",
                keyValue: "USD/JPY");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "CurrencyPairs");

            migrationBuilder.DropColumn(
                name: "TickFrequency",
                table: "CurrencyPairs");
        }
    }
}
