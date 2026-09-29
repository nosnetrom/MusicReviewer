using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicReviewer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoredGenreVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GenreVotes",
                table: "Artists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GenresVersion",
                table: "Artists",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GenreVotes",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "GenresVersion",
                table: "Artists");
        }
    }
}
