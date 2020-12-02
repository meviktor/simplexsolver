using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace simplexapi.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LpTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LPModelAsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SolutionAsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LpTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LpIterationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LpTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IterationLog = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LpIterationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LpIterationLogs_LpTasks_LpTaskId",
                        column: x => x.LpTaskId,
                        principalTable: "LpTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LpIterationLogs_LpTaskId",
                table: "LpIterationLogs",
                column: "LpTaskId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LpIterationLogs");

            migrationBuilder.DropTable(
                name: "LpTasks");
        }
    }
}
