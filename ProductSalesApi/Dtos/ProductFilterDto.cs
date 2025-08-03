namespace ProductSalesApi.Dtos;

public class ProductFilterDto
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
