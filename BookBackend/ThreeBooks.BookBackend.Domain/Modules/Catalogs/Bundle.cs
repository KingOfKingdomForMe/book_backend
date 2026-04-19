namespace ThreeBooks.BookBackend.Domain.Modules.Catalogs;

public sealed class Bundle
{
    public int Id { get; set; }

    public string BundleCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal BundlePrice { get; set; }

    public decimal OriginalPrice { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
