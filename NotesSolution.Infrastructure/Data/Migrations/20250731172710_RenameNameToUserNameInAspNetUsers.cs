using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesSolution.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameNameToUserNameInAspNetUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                           name: "Name",
                           table: "AspNetUsers",
                           newName: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AspNetUsers",
                newName: "Name");
        }
    }
}
