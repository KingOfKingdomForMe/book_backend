namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;

public sealed record AlbumTemplateListItemResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DateTimeOffset UpdatedAtUtc);