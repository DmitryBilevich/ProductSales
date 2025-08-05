namespace ProductSalesApi.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string? SKU { get; set; }
        public string Name { get; set; } = null!;
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public string? Description { get; set; }
        public DateTime? SaleStartDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
