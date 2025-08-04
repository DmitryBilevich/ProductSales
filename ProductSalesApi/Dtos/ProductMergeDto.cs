public class ProductMergeDto
{
    public int ProductID { get; set; }
    public string SKU { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public int QuantityInStock { get; set; }
    public string? Description { get; set; }
    public DateTime? SaleStartDate { get; set; }
    public string OperationType { get; set; } = null!; // Insert / Update / Delete

    public List<Base64ImageDto> Images { get; set; } = new();
}
