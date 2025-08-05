namespace ProductSalesApi.Dtos;

public class ProductExportRequestDto
{
    public string ExportType { get; set; } = "current"; // "current" or "tree"
    public string? SelectedCategory { get; set; }

    // Filter properties (same as ProductFilterDto)
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public List<string>? Categories { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public DateTime? SaleStartDateMin { get; set; }
    public DateTime? SaleStartDateMax { get; set; }
    public List<StockRangeDto>? StockRanges { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 1000; // Export many items
    public string? SortField { get; set; } = "ProductID";
    public int SortOrder { get; set; } = 1;
}