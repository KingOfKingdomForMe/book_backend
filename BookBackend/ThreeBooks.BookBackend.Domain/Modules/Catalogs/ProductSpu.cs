namespace ThreeBooks.BookBackend.Domain.Modules.Catalogs;

public sealed class ProductSpu
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string SpuCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? ContentSource { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
