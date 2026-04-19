namespace ThreeBooks.BookBackend.Domain.Modules.Catalogs;

public sealed class ProductSku
{
    public int Id { get; set; }

    public int SpuId { get; set; }

    public string SkuCode { get; set; } = string.Empty;

    public int? SizeValueId { get; set; }

    public int? BindingValueId { get; set; }

    public int? LayoutValueId { get; set; }

    public int MinPages { get; set; }

    public int? MaxPages { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
