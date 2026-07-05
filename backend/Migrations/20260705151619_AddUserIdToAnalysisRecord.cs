using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vector.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToAnalysisRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AnalysisRecords",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AnalysisRecords");
        }
    }
}
