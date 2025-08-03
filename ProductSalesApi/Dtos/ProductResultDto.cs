namespace ProductSalesApi.Dtos;

public class ProductResultDto
{
    public int ProductID { get; set; }
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int QuantityInStock { get; set; }
    public string? Description { get; set; }
}
