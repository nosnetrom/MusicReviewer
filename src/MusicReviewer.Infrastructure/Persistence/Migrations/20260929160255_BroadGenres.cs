using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicReviewer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BroadGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Styles",
                table: "Artists",
                type: "nvarchar(max)",
                nullable: true);

            // Genres switch from MusicBrainz's specific tags to broad genres. Clear the old ones;
            // CatalogStartupService re-classifies every artist whose Styles is still null.
            migrationBuilder.Sql("DELETE FROM [ArtistGenre]; DELETE FROM [RecordingGenre]; DELETE FROM [Genres];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Styles",
                table: "Artists");
        }
    }
}
