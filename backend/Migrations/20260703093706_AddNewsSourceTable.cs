using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsSourceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsSources",
                columns: table => new
                {
                    SourceName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HistoricalBiasScore = table.Column<double>(type: "float", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArticleCount = table.Column<int>(type: "int", nullable: false),
                    LastEvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsSources", x => x.SourceName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsSources");
        }
    }
}
