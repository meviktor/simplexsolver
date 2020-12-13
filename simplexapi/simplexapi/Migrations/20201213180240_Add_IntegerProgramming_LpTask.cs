using Microsoft.EntityFrameworkCore.Migrations;

namespace simplexapi.Migrations
{
    public partial class Add_IntegerProgramming_LpTask : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IntegerProgramming",
                table: "LpTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegerProgramming",
                table: "LpTasks");
        }
    }
}
