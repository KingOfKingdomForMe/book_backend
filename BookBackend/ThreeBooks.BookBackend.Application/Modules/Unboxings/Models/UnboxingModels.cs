namespace ThreeBooks.BookBackend.Application.Modules.Unboxings.Models;

public sealed record UnboxingListFilter(
    string? LevelCode,
    bool? IsFeatured,
    int PageNumber,
    int PageSize);

public sealed record UnboxingLevelModel(
    string LevelCode,
    string LevelName,
    string? IconUrl);

public sealed record UnboxingMediaModel(
    string MediaType,
    string StorageUrl,
    string? ThumbnailUrl,
    int SortOrder,
    int? Width,
    int? Height);

public sealed record UnboxingWriteMediaCommandModel(
    string MediaType,
    string StorageUrl,
    string? ThumbnailUrl,
    int SortOrder,
    int? Width,
    int? Height);

public sealed record UnboxingCreateCommandModel(
    long UserId,
    string PostNo,
    string? AuthorName,
    string? AuthorAvatarUrl,
    long? OrderItemId,
    long? ProjectVersionId,
    string? ProductLabel,
    string BookTitle,
    string? LevelCode,
    string Title,
    string ContentText,
    int Status,
    bool IsFeatured,
    DateTime? PublishedAtUtc,
    IReadOnlyCollection<UnboxingWriteMediaCommandModel> Media,
    IReadOnlyCollection<string> Tags);

public sealed record UnboxingUpdateCommandModel(
    string? AuthorName,
    string? AuthorAvatarUrl,
    long? OrderItemId,
    long? ProjectVersionId,
    string? ProductLabel,
    string BookTitle,
    string? LevelCode,
    string Title,
    string ContentText,
    int Status,
    bool IsFeatured,
    DateTime? PublishedAtUtc,
    IReadOnlyCollection<UnboxingWriteMediaCommandModel> Media,
    IReadOnlyCollection<string> Tags);

public sealed record UnboxingWriteResultModel(
    long PostId,
    string PostNo,
    long UserId,
    int Status,
    bool IsFeatured,
    int MediaCount,
    int TagCount,
    DateTime? PublishedAtUtc);

public sealed record UnboxingListItemQueryModel(
    long PostId,
    string PostNo,
    string? AuthorName,
    string? AuthorAvatarUrl,
    string? Title,
    string? BookTitle,
    string? ContentText,
    string? ProductLabel,
    UnboxingLevelModel? Level,
    string? CoverImageUrl,
    string? CoverThumbnailUrl,
    bool IsFeatured,
    DateTime PublishedAtUtc,
    IReadOnlyCollection<string> Tags);

public sealed record UnboxingListQueryResultModel(
    IReadOnlyCollection<UnboxingListItemQueryModel> Items,
    int TotalCount);

public sealed record UnboxingDetailQueryModel(
    long PostId,
    string PostNo,
    string? AuthorName,
    string? AuthorAvatarUrl,
    string? Title,
    string? BookTitle,
    string? ContentText,
    string? ProductLabel,
    UnboxingLevelModel? Level,
    bool IsFeatured,
    DateTime PublishedAtUtc,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<UnboxingMediaModel> Media);