using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iscuissatis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoBusca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchTerms",
                table: "Products",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchTerms",
                table: "Products");
        }
    }
}
