using Microsoft.EntityFrameworkCore.Migrations;

namespace Anabasis.EntityFramework.Tests.Integration.Migrations
{
    public partial class InitialSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyPairs",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Currency1Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Currency2Code = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyPairs", x => x.Code);
                    table.ForeignKey(
                        name: "FK_CurrencyPairs_Currencies_Currency1Code",
                        column: x => x.Currency1Code,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyPairs_Currencies_Currency2Code",
                        column: x => x.Currency2Code,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { "USD", "United States dollar" },
                    { "BRL", "Brazilian real" },
                    { "TWD", "New Taiwan dollar" },
                    { "DKK", "Danish krone" },
                    { "PLN", "Polish złoty" },
                    { "THB", "Thai baht" },
                    { "IDR", "Indonesian rupiah" },
                    { "TRY", "Turkish lira" },
                    { "HUF", "Hungarian forint" },
                    { "ILS", "Israeli new shekel" },
                    { "CLP", "Chilean peso" },
                    { "PHP", "Philippine peso" },
                    { "AED", "UAE dirham" },
                    { "COP", "Colombian peso" },
                    { "SAR", "Saudi riyal" },
                    { "CZK", "Czech koruna" },
                    { "MYR", "Malaysian ringgit" },
                    { "ZAR", "South African rand" },
                    { "INR", "Indian rupee" },
                    { "EUR", "Euro" },
                    { "JPY", "Japanese yen" },
                    { "GBP", "Pound sterling" },
                    { "AUD", "Australian dollar" },
                    { "CAD", "Canadian dollar" },
                    { "CHF", "Swiss franc" },
                    { "RUB", "Russian ruble" },
                    { "CNY", "Renminbi" },
                    { "NZD", "New Zealand dollar" },
                    { "SEK", "Swedish krona" },
                    { "KRW", "South Korean won" },
                    { "SGD", "Singapore dollar" },
                    { "NOK", "Norwegian krone" },
                    { "MXN", "Mexican peso" },
                    { "HKD", "Hong Kong dollar" },
                    { "RON", "Romanian leu" }
                });

            migrationBuilder.InsertData(
                table: "CurrencyPairs",
                columns: new[] { "Code", "Currency1Code", "Currency2Code" },
                values: new object[] { "EUR/USD", "EUR", "USD" });

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Name_Code",
                table: "Currencies",
                columns: new[] { "Name", "Code" },
                unique: true,
                filter: "[Name] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyPairs_Currency1Code_Currency2Code",
                table: "CurrencyPairs",
                columns: new[] { "Currency1Code", "Currency2Code" },
                unique: true,
                filter: "[Currency1Code] IS NOT NULL AND [Currency2Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyPairs_Currency2Code",
                table: "CurrencyPairs",
                column: "Currency2Code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyPairs");

            migrationBuilder.DropTable(
                name: "Currencies");
        }
    }
}
