namespace MusicReviewer.Domain.Catalog;

/// <summary>Progress of importing an entity's data from external sources.</summary>
public enum SyncStatus
{
    NotSynced = 0,
    Syncing = 1,
    Ready = 2,
    Failed = 3,
}

/// <summary>Groups of release types that are imported and filtered together.</summary>
[Flags]
public enum ReleaseCategories
{
    None = 0,

    /// <summary>Albums and EPs with no secondary type.</summary>
    Studio = 1 << 0,

    Live = 1 << 1,
    Compilation = 1 << 2,
}
