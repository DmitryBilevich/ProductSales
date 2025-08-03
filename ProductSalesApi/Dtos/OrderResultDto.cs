namespace ProductSalesApi.Dtos
{
    public class OrderResultDto
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; } = null!;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItemResultDto> Items { get; set; } = new();
    }
}
