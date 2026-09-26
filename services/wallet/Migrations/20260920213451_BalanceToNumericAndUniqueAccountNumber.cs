using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wallet.Migrations
{
    /// <inheritdoc />
    public partial class BalanceToNumericAndUniqueAccountNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres will not cast text to numeric on its own; AlterColumn emits a
            // plain TYPE change and fails without the USING clause.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Wallets""
                ALTER COLUMN ""CachedBalance"" TYPE numeric(18,2)
                USING NULLIF(""CachedBalance"", '')::numeric(18,2),
                ALTER COLUMN ""CachedBalance"" SET DEFAULT 0,
                ALTER COLUMN ""CachedBalance"" SET NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_AccountNumber",
                table: "Wallets",
                column: "AccountNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_AccountNumber",
                table: "Wallets");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Wallets""
                ALTER COLUMN ""CachedBalance"" DROP DEFAULT,
                ALTER COLUMN ""CachedBalance"" TYPE text
                USING ""CachedBalance""::text;");
        }
    }
}
