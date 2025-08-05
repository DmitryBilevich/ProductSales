namespace ProductSalesApi.Dtos;

public class ProductImportRowDto
{
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Price { get; set; } = string.Empty; // String to handle parsing
    public string QuantityInStock { get; set; } = string.Empty; // String to handle parsing
    public string? Description { get; set; }
    public string? SaleStartDate { get; set; } // String to handle parsing
    public int RowNumber { get; set; }
}