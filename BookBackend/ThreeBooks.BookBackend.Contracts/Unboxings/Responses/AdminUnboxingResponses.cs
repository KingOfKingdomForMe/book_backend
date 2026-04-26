namespace ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

public sealed record AdminUnboxingListItemResponse(
    long PostId,
    string PostNo,
    long UserId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string? Title,
    string? BookTitle,
    string? ProductLabel,
    int Status,
    UnboxingLevelResponse? Level,
    bool IsFeatured,
    string? CoverImageUrl,
    string? CoverThumbnailUrl,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<string> Tags);

public sealed record AdminUnboxingDetailResponse(
    long PostId,
    string PostNo,
    long UserId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string? Title,
    string? BookTitle,
    string? ContentText,
    string? ProductLabel,
    int Status,
    UnboxingLevelResponse? Level,
    bool IsFeatured,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<UnboxingMediaResponse> Media);