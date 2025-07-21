using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessTrackerBack.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionAndNotesToWorkoutHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WorkoutHistory",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "WorkoutHistory",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "WorkoutHistory");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "WorkoutHistory");
        }
    }
}
