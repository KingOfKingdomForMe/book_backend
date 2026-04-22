namespace ThreeBooks.BookBackend.Contracts.AlbumTemplates.Responses;

public sealed record CreateAlbumTemplateResponse(
    long TemplateId,
    string TemplateCode,
    bool IsActive,
    string? PreviewUrl);