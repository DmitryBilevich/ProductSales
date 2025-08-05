namespace ProductSalesApi.Dtos;

public class ProductImportStagingDto
{
    public int StagingID { get; set; }
    public string OperationType { get; set; } = string.Empty; // "Insert" or "Update"
    public string? SKU { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int QuantityInStock { get; set; }
    public string? Description { get; set; }
    public DateTime? SaleStartDate { get; set; }
    public int? ExistingProductID { get; set; }
    public string? ValidationErrors { get; set; }
    public int RowNumber { get; set; }
    public DateTime ModifiedAt { get; set; }

    // Current values from existing product (for updates)
    public string? CurrentName { get; set; }
    public string? CurrentCategory { get; set; }
    public decimal? CurrentPrice { get; set; }
    public int? CurrentQuantityInStock { get; set; }
    public string? CurrentDescription { get; set; }
    public DateTime? CurrentSaleStartDate { get; set; }
}