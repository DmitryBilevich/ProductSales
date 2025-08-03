namespace ProductSalesApi.Dtos;

public class OrderFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? CustomerName { get; set; }
    public List<int> ProductIDs { get; set; } = new();
}
