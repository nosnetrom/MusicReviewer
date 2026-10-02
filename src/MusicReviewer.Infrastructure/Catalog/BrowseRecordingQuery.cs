using System.Data;
using Microsoft.EntityFrameworkCore;
using MusicReviewer.Application.Abstractions;
using MusicReviewer.Domain.Catalog;
using MusicReviewer.Infrastructure.Persistence;

namespace MusicReviewer.Infrastructure.Catalog;

public sealed class BrowseRecordingQuery(MusicReviewerDbContext db) : IBrowseRecordingQuery
{
    private const string Sql = """
        WITH Filtered AS
        (
            SELECT r.MusicBrainzId, r.ArtistId, r.NotabilityScore, r.FirstReleaseDate, r.Title
            FROM Recordings AS r
            INNER JOIN Artists AS a ON a.Id = r.ArtistId
            WHERE a.LastSyncedUtc IS NOT NULL
                AND r.SecondaryTypes = 0
                AND r.PrimaryType = @AlbumType
                AND (@Genre IS NULL OR EXISTS
                (
                    SELECT 1
                    FROM ArtistGenre AS ag
                    INNER JOIN Genres AS g ON g.Id = ag.GenresId
                    WHERE ag.ArtistsId = a.Id AND g.Slug = @Genre
                ))
                AND (@DecadeStart IS NULL OR (r.FirstReleaseYear >= @DecadeStart AND r.FirstReleaseYear < @DecadeStart + 10))
        ),
        ArtistRanked AS
        (
            SELECT *, ROW_NUMBER() OVER
            (
                PARTITION BY ArtistId
                ORDER BY NotabilityScore DESC, CASE WHEN FirstReleaseDate IS NULL THEN 1 ELSE 0 END,
                    FirstReleaseDate, Title, MusicBrainzId
            ) AS ArtistPosition
            FROM Filtered
        ),
        BrowseRanked AS
        (
            SELECT MusicBrainzId, ROW_NUMBER() OVER
            (
                ORDER BY (ArtistPosition - 1) / 3, NotabilityScore DESC,
                    CASE WHEN FirstReleaseDate IS NULL THEN 1 ELSE 0 END,
                    FirstReleaseDate, Title, MusicBrainzId
            ) AS BrowsePosition
            FROM ArtistRanked
        ),
        Total AS (SELECT COUNT_BIG(*) AS Total FROM Filtered)
        SELECT page.MusicBrainzId, Total.Total
        FROM Total
        LEFT JOIN BrowseRanked AS page
            ON page.BrowsePosition > @Offset AND page.BrowsePosition <= @EndPosition
        ORDER BY page.BrowsePosition;
        """;

    public async Task<BrowseRecordingPage> GetPageAsync(
        string? genre,
        int? decadeStart,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = Sql;
            AddParameter(command, "@Genre", DbType.String, genre);
            AddParameter(command, "@DecadeStart", DbType.Int32, decadeStart);
            AddParameter(command, "@Offset", DbType.Int64, (long)offset);
            AddParameter(command, "@EndPosition", DbType.Int64, (long)offset + limit);
            AddParameter(command, "@AlbumType", DbType.Int32, (int)ReleaseType.Album);

            var ids = new List<Guid>(limit);
            var total = 0;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                    ids.Add(reader.GetGuid(0));
                total = checked((int)reader.GetInt64(1));
            }

            return new BrowseRecordingPage(ids, total);
        }
        finally
        {
            if (closeConnection)
                await db.Database.CloseConnectionAsync();
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, DbType type, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}