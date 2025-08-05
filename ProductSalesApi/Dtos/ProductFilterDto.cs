namespace ProductSalesApi.Dtos;

public class ProductFilterDto
{
    // Search fields
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public List<string>? Categories { get; set; }

    // Price range filters
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }

    // Stock filters (client-defined ranges)
    public List<StockRangeDto>? StockRanges { get; set; }

    // Sale Start Date range filters
    public DateTime? SaleStartDateMin { get; set; }
    public DateTime? SaleStartDateMax { get; set; }

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Sorting
    public string? SortField { get; set; }
    public int SortOrder { get; set; } = 1; // 1 = ASC, -1 = DESC
}
