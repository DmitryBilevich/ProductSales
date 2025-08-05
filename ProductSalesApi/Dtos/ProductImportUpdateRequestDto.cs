namespace ProductSalesApi.Dtos;

public class ProductImportUpdateRequestDto
{
    public int StagingID { get; set; }
    public string? SKU { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int QuantityInStock { get; set; }
    public string? Description { get; set; }
    public DateTime? SaleStartDate { get; set; }
}