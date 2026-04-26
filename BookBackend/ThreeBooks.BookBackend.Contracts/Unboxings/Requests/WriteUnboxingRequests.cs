namespace ThreeBooks.BookBackend.Contracts.Unboxings.Requests;

public sealed record CreateUnboxingRequest(
    long UserId,
    string Title,
    string ContentText,
    string? PostNo = null,
    string? AuthorName = null,
    string? AuthorAvatarUrl = null,
    string? BookTitle = null,
    string? ProductLabel = null,
    string? LevelCode = null,
    long? OrderItemId = null,
    long? ProjectVersionId = null,
    bool IsFeatured = false,
    int Status = 0,
    DateTime? PublishedAtUtc = null,
    IReadOnlyCollection<WriteUnboxingMediaRequest>? Media = null,
    IReadOnlyCollection<string>? Tags = null);

public sealed record UpdateUnboxingRequest(
    string Title,
    string ContentText,
    string? AuthorName = null,
    string? AuthorAvatarUrl = null,
    string? BookTitle = null,
    string? ProductLabel = null,
    string? LevelCode = null,
    long? OrderItemId = null,
    long? ProjectVersionId = null,
    bool IsFeatured = false,
    int Status = 0,
    DateTime? PublishedAtUtc = null,
    IReadOnlyCollection<WriteUnboxingMediaRequest>? Media = null,
    IReadOnlyCollection<string>? Tags = null);

public sealed record WriteUnboxingMediaRequest(
    string MediaType,
    string StorageUrl,
    string? ThumbnailUrl = null,
    int? SortOrder = null,
    int? Width = null,
    int? Height = null);