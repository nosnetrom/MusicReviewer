using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicReviewer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_Status_CreatedUtc",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_Type_TargetId",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_Artists_Name",
                table: "Artists");

            migrationBuilder.AddColumn<string>(
                name: "ArtistCredit",
                table: "Recordings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetailsSyncStatus",
                table: "Recordings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DetailsSyncedUtc",
                table: "Recordings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepresentativeReleaseId",
                table: "Recordings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikidataId",
                table: "Recordings",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WikidataSitelinks",
                table: "Recordings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikipediaTitle",
                table: "Recordings",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NotBeforeUtc",
                table: "IngestionJobs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Parameter",
                table: "IngestionJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "IngestionJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImportedCategories",
                table: "Artists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Artists",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "Artists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WikidataId",
                table: "Artists",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WikidataSitelinks",
                table: "Artists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikipediaTitle",
                table: "Artists",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_Active",
                table: "IngestionJobs",
                columns: new[] { "Type", "TargetId", "Parameter" },
                unique: true,
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_Status_Priority_NotBeforeUtc_CreatedUtc",
                table: "IngestionJobs",
                columns: new[] { "Status", "Priority", "NotBeforeUtc", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_NormalizedName",
                table: "Artists",
                column: "NormalizedName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_Active",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_Status_Priority_NotBeforeUtc_CreatedUtc",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_Artists_NormalizedName",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "ArtistCredit",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "DetailsSyncStatus",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "DetailsSyncedUtc",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "RepresentativeReleaseId",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "WikidataId",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "WikidataSitelinks",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "WikipediaTitle",
                table: "Recordings");

            migrationBuilder.DropColumn(
                name: "NotBeforeUtc",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "Parameter",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "ImportedCategories",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "WikidataId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "WikidataSitelinks",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "WikipediaTitle",
                table: "Artists");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_Status_CreatedUtc",
                table: "IngestionJobs",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_Type_TargetId",
                table: "IngestionJobs",
                columns: new[] { "Type", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Name",
                table: "Artists",
                column: "Name");
        }
    }
}
