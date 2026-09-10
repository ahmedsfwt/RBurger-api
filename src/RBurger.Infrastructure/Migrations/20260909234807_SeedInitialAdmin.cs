using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBurger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "Id", "CreatedAt", "FullName", "IsActive", "PasswordHash", "Username" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Super Admin", true, "AQAAAAIAAYagAAAAEERBAFafF7v9BPn4hGqNSJZR0APqsKFK2LZOfawCcBXa+gm2CaYbmm1KZYmN7NijCQ==", "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
