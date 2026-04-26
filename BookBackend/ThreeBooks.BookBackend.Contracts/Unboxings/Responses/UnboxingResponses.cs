namespace ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

public sealed record UnboxingLevelResponse(
    string LevelCode,
    string LevelName,
    string? IconUrl);

public sealed record UnboxingListItemResponse(
    long PostId,
    string PostNo,
    string AuthorName,
    string? AuthorAvatarUrl,
    string? Title,
    string? BookTitle,
    string? Excerpt,
    string? ProductLabel,
    UnboxingLevelResponse? Level,
    string? CoverImageUrl,
    string? CoverThumbnailUrl,
    bool IsFeatured,
    DateTime PublishedAtUtc,
    IReadOnlyCollection<string> Tags);

public sealed record UnboxingDetailResponse(
    long PostId,
    string PostNo,
    string AuthorName,
    string? AuthorAvatarUrl,
    string? Title,
    string? BookTitle,
    string? ContentText,
    string? ProductLabel,
    UnboxingLevelResponse? Level,
    bool IsFeatured,
    DateTime PublishedAtUtc,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<UnboxingMediaResponse> Media);

public sealed record UnboxingMediaResponse(
    string MediaType,
    string StorageUrl,
    string? ThumbnailUrl,
    int SortOrder,
    int? Width,
    int? Height);