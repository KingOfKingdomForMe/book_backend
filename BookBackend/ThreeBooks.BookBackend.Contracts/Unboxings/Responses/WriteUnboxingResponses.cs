namespace ThreeBooks.BookBackend.Contracts.Unboxings.Responses;

public sealed record SaveUnboxingResponse(
    long PostId,
    string PostNo,
    long UserId,
    int Status,
    bool IsFeatured,
    string? LevelCode,
    int MediaCount,
    int TagCount,
    DateTime? PublishedAtUtc);