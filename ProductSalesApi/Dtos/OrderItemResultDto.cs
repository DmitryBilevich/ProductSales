namespace ProductSalesApi.Dtos
{
    public class OrderItemResultDto
    {
        public int OrderItemID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public int ProductID { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Category { get; set; }
        public decimal ProductCurrentPrice { get; set; }
    }
}
