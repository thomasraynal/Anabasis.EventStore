using Microsoft.EntityFrameworkCore.Migrations;

namespace Anabasis.EntityFramework.Tests.Integration.Migrations
{
    public partial class AddCurrencyTableDataset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "AUD", "Australian dollar" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "BRL", "Brazilian real" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "CAD", "Canadian dollar" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "CLP", "Chilean peso" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "COP", "Colombian peso" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "CZK", "Czech koruna" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "DKK", "Danish krone" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "EUR", "Euro" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "HKD", "Hong Kong dollar" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "HUF", "Hungarian forint" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "INR", "Indian rupee" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "IDR", "Indonesian rupiah" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "ILS", "Israeli new shekel" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "JPY", "Japanese yen" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "MYR", "Malaysian ringgit" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "MXN", "Mexican peso" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "TWD", "New Taiwan dollar" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "NZD", "New Zealand dollar" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "NOK", "Norwegian krone" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "PHP", "Philippine peso" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "PLN", "Polish złoty" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "GBP", "Pound sterling" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "CNY", "Renminbi" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "RON", "Romanian leu" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "RUB", "Russian ruble" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "SAR", "Saudi riyal" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "SGD", "Singapore dollar" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "ZAR", "South African rand" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "KRW", "South Korean won" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "SEK", "Swedish krona" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "CHF", "Swiss franc" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "THB", "Thai baht" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "TRY", "Turkish lira" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "AED", "UAE dirham" });

            migrationBuilder.DeleteData(
                table: "Currencies",
                keyColumns: new[] { "Code", "Name" },
                keyValues: new object[] { "USD", "United States dollar" });
        }
    }
}
