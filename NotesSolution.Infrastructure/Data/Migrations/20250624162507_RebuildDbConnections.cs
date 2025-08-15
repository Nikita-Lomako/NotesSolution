using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotesSolution.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebuildDbConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NoteTag_Notes_NoteId",
                table: "NoteTag");

            migrationBuilder.RenameColumn(
                name: "NoteId",
                table: "NoteTag",
                newName: "NotesId");

            migrationBuilder.AddForeignKey(
                name: "FK_NoteTag_Notes_NotesId",
                table: "NoteTag",
                column: "NotesId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NoteTag_Notes_NotesId",
                table: "NoteTag");

            migrationBuilder.RenameColumn(
                name: "NotesId",
                table: "NoteTag",
                newName: "NoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_NoteTag_Notes_NoteId",
                table: "NoteTag",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
