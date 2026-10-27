using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iscuissatis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailsPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SentOrderEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    To = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentOrderEmails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SentOrderEmails_OrderId_Kind",
                table: "SentOrderEmails",
                columns: new[] { "OrderId", "Kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SentOrderEmails");
        }
    }
}
