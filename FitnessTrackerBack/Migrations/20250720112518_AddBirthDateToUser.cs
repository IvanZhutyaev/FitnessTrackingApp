using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitnessTrackerBack.Migrations
{
    /// <inheritdoc />
    public partial class AddBirthDateToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Age",
                table: "Users",
                newName: "BirthDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BirthDate",
                table: "Users",
                newName: "Age");
        }
    }
}
